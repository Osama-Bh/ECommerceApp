using ECommerceApp.Data;
using ECommerceApp.DTOs;
using ECommerceApp.DTOs.ProductDTOs;
using ECommerceApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data.SqlTypes;
using System.Security.Principal;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace ECommerceApp.Services
{
    public interface IProductService
    {
        Task<ApiResponse<ProductResponseDTO>> CreateProductAsync(ProductCreateDTO productCreateDTO);
        Task<ApiResponse<ProductResponseDTO>> GetProductByIdAsync(int productId);
        Task<ApiResponse<ConfirmationResponseDTO>> UpdateProductAsync(ProductUpdateDTO productUpdateDTO);
        Task<ApiResponse<ConfirmationResponseDTO>> DeleteProductAsync(int productId);
        Task<ApiResponse<List<ProductResponseDTO>>> GetAllProductsAsync();
        Task<ApiResponse<List<ProductResponseDTO>>> GetAllProductsByCategoryAsync(int categoryId);
        Task<ApiResponse<ConfirmationResponseDTO>> UpdateProductStatusAsync(ProductStatusUpdateDTO productStatusUpdateDTO);
    }

    public class ProductService : IProductService
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ProductService(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }


        public async Task<ApiResponse<ProductResponseDTO>> CreateProductAsync(ProductCreateDTO productCreateDTO)
        {
            try
            {
                //check if the product name is already exists
                if (await _context.TbProducts.AnyAsync(p => p.Name.ToLower() == productCreateDTO.Name.ToLower()))
                    return new ApiResponse<ProductResponseDTO>(400, "Product name already exists.");

                //check if category is exists or not ot should be exsisted
                if (!await _context.TbCategories.AnyAsync(c => c.Id == productCreateDTO.CategoryId))
                    return new ApiResponse<ProductResponseDTO>(400, "Specified category does not exist.");

                // Save image if uploaded
                string imageUrl = null;
                if (productCreateDTO.ImageFile != null && productCreateDTO.ImageFile.Length > 0)
                {
                    imageUrl = await SaveImageAsync(productCreateDTO.ImageFile);
                }

                // map data from DTO to Product object to save it in the Database
                var product = new Product()
                {
                    Name = productCreateDTO.Name,
                    Description = productCreateDTO.Description,
                    Price = productCreateDTO.Price,
                    StockQuantity = productCreateDTO.StockQuantity,
                    ImageUrl = imageUrl != null ? imageUrl : "",
                    DiscountPercentage = productCreateDTO.DiscountPercentage,
                    CategoryId = productCreateDTO.CategoryId,
                    IsAvailable = true
                };

                _context.TbProducts.Add(product);
                await _context.SaveChangesAsync();

                //prepare response DTO
                var response = new ProductResponseDTO()
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    StockQuantity = product.StockQuantity,
                    ImageUrl = product.ImageUrl,
                    DiscountPercentage = product.DiscountPercentage,
                    CategoryId = product.CategoryId,
                    IsAvailable = product.IsAvailable
                };

                return new ApiResponse<ProductResponseDTO>(201, response);
            }
            catch (Exception ex)
            {
                return new ApiResponse<ProductResponseDTO>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<ProductResponseDTO>> GetProductByIdAsync(int productId)
        {
            try
            {
                var product = await _context.TbProducts.SingleOrDefaultAsync(p => p.Id == productId);

                if (product == null)
                    return new ApiResponse<ProductResponseDTO>(404, $"Product with Id {productId} not found");

                //prepare response DTO
                var response = new ProductResponseDTO()
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    StockQuantity = product.StockQuantity,
                    ImageUrl = product.ImageUrl,
                    DiscountPercentage = product.DiscountPercentage,
                    CategoryId = product.CategoryId,
                    IsAvailable = product.IsAvailable
                };

                return new ApiResponse<ProductResponseDTO>(200, response);
            }catch (Exception ex)
            {
                return new ApiResponse<ProductResponseDTO>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<ConfirmationResponseDTO>> UpdateProductAsync(ProductUpdateDTO productUpdateDTO)
        {
            try
            {
                var product = await _context.TbProducts.SingleOrDefaultAsync(p => p.Id == productUpdateDTO.Id);

                if (product == null)
                    return new ApiResponse<ConfirmationResponseDTO>(404, $"Product with Id {productUpdateDTO.Id} not found");

                // Check if the new product name already exists (case-insensitive), excluding the current product
                if (await _context.TbProducts.AnyAsync(p => p.Name.ToLower() == productUpdateDTO.Name.ToLower()
                && p.Id != product.Id))
                {
                    return new ApiResponse<ConfirmationResponseDTO>(400, "Another product with the same name already exists.");
                }
                // Check if Category exists
                if (!await _context.TbCategories.AnyAsync(cat => cat.Id == productUpdateDTO.CategoryId))
                {
                    return new ApiResponse<ConfirmationResponseDTO>(400, "Specified category does not exist.");
                }


                // Update product properties manually
                product.Name = productUpdateDTO.Name;
                product.Description = productUpdateDTO.Description;
                product.Price = productUpdateDTO.Price;
                product.StockQuantity = productUpdateDTO.StockQuantity;
                product.DiscountPercentage = productUpdateDTO.DiscountPercentage;
                product.CategoryId = productUpdateDTO.CategoryId;

                //Image handling
                if (productUpdateDTO.ImageFile != null && productUpdateDTO.ImageFile.Length > 0)
                {
                    if (!string.IsNullOrEmpty(product.ImageUrl))
                    {
                        var oldImagePath = Path.Combine(_environment.WebRootPath, product.ImageUrl.TrimStart('/'));
                        if (File.Exists(oldImagePath)) File.Delete(oldImagePath);
                    }
                    product.ImageUrl = await SaveImageAsync(productUpdateDTO.ImageFile);
                }

                await _context.SaveChangesAsync();

                // Prepare confirmation message
                var message = new ConfirmationResponseDTO()
                {
                    Message = $"Product with Id {product.Id} updated successfully"
                };

                return new ApiResponse<ConfirmationResponseDTO>(200, message);
            }
            catch (Exception ex)
            {
                // Log the exception
                return new ApiResponse<ConfirmationResponseDTO>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<ConfirmationResponseDTO>> DeleteProductAsync(int productId)
        {
            try
            {
                var product = await _context.TbProducts.FirstOrDefaultAsync(p => p.Id == productId);

                if (product == null)
                    return new ApiResponse<ConfirmationResponseDTO>(404, $"Product with Id {productId} not found");

                //soft delete
                product.IsAvailable = false;
                await _context.SaveChangesAsync();

                return new ApiResponse<ConfirmationResponseDTO>(200,
                    new ConfirmationResponseDTO() { Message = $"Product with Id {productId} deleted successfully" });
            }
            catch (Exception ex)
            {
                // Log the exception
                return new ApiResponse<ConfirmationResponseDTO>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<ProductResponseDTO>>> GetAllProductsAsync()
        {
            try
            {
                var products = await _context.TbProducts.Where(p => p.IsAvailable == true).ToListAsync();

                if (products == null)
                    return new ApiResponse<List<ProductResponseDTO>>(404, "There aren't any products is the system");

                var lstProductDTO = products.Select(products => new ProductResponseDTO()
                {
                    Id = products.Id,
                    Name = products.Name,
                    Description = products.Description,
                    Price = products.Price,
                    StockQuantity = products.StockQuantity,
                    ImageUrl = products.ImageUrl,
                    DiscountPercentage = products.DiscountPercentage,
                    CategoryId = products.CategoryId,
                    IsAvailable = products.IsAvailable,

                }).ToList();

                return new ApiResponse<List<ProductResponseDTO>>(200, lstProductDTO);
            }
            catch(Exception ex)
            {
                // Log the exception
                return new ApiResponse<List<ProductResponseDTO>>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<ProductResponseDTO>>> GetAllProductsByCategoryAsync(int categoryId)
        {
            try
            {
                var products = await _context.TbProducts.Where(p => p.IsAvailable == true
                && p.CategoryId == categoryId).ToListAsync();

                if (products == null)
                    return new ApiResponse<List<ProductResponseDTO>>(404, "There aren't any products is the system");

                var lstProductDTO = products.Select(products => new ProductResponseDTO()
                {
                    Id = products.Id,
                    Name = products.Name,
                    Description = products.Description,
                    Price = products.Price,
                    StockQuantity = products.StockQuantity,
                    ImageUrl = products.ImageUrl,
                    DiscountPercentage = products.DiscountPercentage,
                    CategoryId = products.CategoryId,
                    IsAvailable = products.IsAvailable,

                }).ToList();

                return new ApiResponse<List<ProductResponseDTO>>(200, lstProductDTO);
            }
            catch (Exception ex)
            {
                // Log the exception
                return new ApiResponse<List<ProductResponseDTO>>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<ConfirmationResponseDTO>> UpdateProductStatusAsync(ProductStatusUpdateDTO productStatusUpdateDTO)
        {
            try
            {
                var product = await _context.TbProducts
                .FirstOrDefaultAsync(p => p.Id == productStatusUpdateDTO.ProductId);

                if (product == null)
                {
                    return new ApiResponse<ConfirmationResponseDTO>(404, "Product not found.");
                }

                product.IsAvailable = productStatusUpdateDTO.IsAvailable;
                await _context.SaveChangesAsync();

                // Prepare confirmation message
                var confirmationMessage = new ConfirmationResponseDTO
                {
                    Message = $"Product with Id {productStatusUpdateDTO.ProductId} Status Updated successfully."
                };
                return new ApiResponse<ConfirmationResponseDTO>(200, confirmationMessage);
            }
            catch (Exception ex)
            {
                // Log the exception
                return new ApiResponse<ConfirmationResponseDTO>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }

        private async Task<string> SaveImageAsync(IFormFile file)
        {
            var uploadPath = Path.Combine(_environment.WebRootPath, "uploads");

            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            var uniqueFileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadPath, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/{uniqueFileName}";
        }
    }
}
