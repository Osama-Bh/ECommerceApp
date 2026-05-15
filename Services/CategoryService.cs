using ECommerceApp.Data;
using ECommerceApp.DTOs;
using ECommerceApp.DTOs.CategoryDTOs;
using ECommerceApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices.ObjectiveC;
using System.Threading.Tasks;

namespace ECommerceApp.Services
{
    public interface ICategoryService
    {
        Task<ApiResponse<CategoryResponseDTO>> CreateCategoryAsync(CategoryCreateDTO categoryCreateDTO);

        Task<ApiResponse<CategoryResponseDTO>> GetCategoryByIdAsync(int categoryId);

        Task<ApiResponse<ConfirmationResponseDTO>> UpdateCategoryAsync(CategoryUpdateDTO categoryUpdateDTO);

        Task<ApiResponse<ConfirmationResponseDTO>> DeleteCategoryAsync(int categoryId);

        Task<ApiResponse<List<CategoryResponseDTO>>> GetAllCategoriesAsync();
    }

    public class CategoryService : ICategoryService
    {
        private readonly AppDbContext _context;

        public CategoryService(AppDbContext ctx)
        {
            _context = ctx;
        }

        public async Task<ApiResponse<CategoryResponseDTO>> CreateCategoryAsync(CategoryCreateDTO categoryCreateDTO)
        {
            try
            {
                //check if the category name is already exists
                if (await _context.TbCategories.AnyAsync(c => c.Name == categoryCreateDTO.Name))
                {
                    return new ApiResponse<CategoryResponseDTO>(400, $"Category Name {categoryCreateDTO.Name} is already exists");
                }

                var category = new Category()
                {
                    Name = categoryCreateDTO.Name,
                    Description = categoryCreateDTO.Description,
                    IsActive = true
                };

                await _context.TbCategories.AddAsync(category);
                await _context.SaveChangesAsync();

                var response = new CategoryResponseDTO()
                {
                    Id = category.Id,
                    Name = category.Name,
                    Description = category.Description,
                    IsActive = true
                };

                return new ApiResponse<CategoryResponseDTO>(201, response);
            }catch (Exception ex)
            {
                // Log the exception (implementation depends on your logging setup)
                return new ApiResponse<CategoryResponseDTO>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<CategoryResponseDTO>> GetCategoryByIdAsync(int categoryId)
        {
            try
            {
                var category = await _context.TbCategories.FirstOrDefaultAsync(c => c.Id == categoryId
                && c.IsActive == true);

                if (category == null)
                {
                    return new ApiResponse<CategoryResponseDTO>(404, "Category not found");
                }

                //Prepare response
                var responseDTO = new CategoryResponseDTO()
                {
                    Id = category.Id,
                    Name = category.Name,
                    Description = category.Description,
                    IsActive = category.IsActive
                };

                return new ApiResponse<CategoryResponseDTO>(200, responseDTO);
            }
            catch (Exception ex)
            {
                // Log the exception (implementation depends on your logging setup)
                return new ApiResponse<CategoryResponseDTO>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<ConfirmationResponseDTO>> UpdateCategoryAsync(CategoryUpdateDTO categoryUpdateDTO)
        {
            try
            {
                var category = await _context.TbCategories.FirstOrDefaultAsync(c => c.Id == categoryUpdateDTO.Id
                && c.IsActive == true);

                if (category == null)
                {
                    return new ApiResponse<ConfirmationResponseDTO>(404, "Category not found");
                }

                // Check if the new category name already exists (excluding current category)
                if (await _context.TbCategories.AnyAsync(c => c.Name.ToLower() == categoryUpdateDTO.Name.ToLower() && c.Id != categoryUpdateDTO.Id))
                {
                    return new ApiResponse<ConfirmationResponseDTO>(400, "Another category with the same name already exists.");
                }

                category.Name = categoryUpdateDTO.Name;
                category.Description = categoryUpdateDTO.Description;

                await _context.SaveChangesAsync();

                return new ApiResponse<ConfirmationResponseDTO>(200,
                    new ConfirmationResponseDTO() { Message = $"Category with Id {category.Id} updated successfully" });
            }
            catch (Exception ex)
            {
                // Log the exception (implementation depends on your logging setup)
                return new ApiResponse<ConfirmationResponseDTO>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<ConfirmationResponseDTO>> DeleteCategoryAsync(int categoryId)
        {
            try
            {
                var category = await _context.TbCategories.FirstOrDefaultAsync(c => c.Id == categoryId
                && c.IsActive == true);

                if (category == null)
                {
                    return new ApiResponse<ConfirmationResponseDTO>(404, $"Category with Id {categoryId} not found");
                }

                category.IsActive = false;

                await _context.SaveChangesAsync();

                return new ApiResponse<ConfirmationResponseDTO>(200,
                    new ConfirmationResponseDTO() { Message = $"Category with Id {categoryId} deleted successfully" });
            }
            catch (Exception ex)
            {
                // Log the exception (implementation depends on your logging setup)
                return new ApiResponse<ConfirmationResponseDTO>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<CategoryResponseDTO>>> GetAllCategoriesAsync()
        {
            try
            {
                var categories = await _context.TbCategories.Where(c => c.IsActive == true).ToListAsync();

                if (categories == null)
                    return new ApiResponse<List<CategoryResponseDTO>>(404, "There are no categories in the system");

                var lstCategories = categories.Select(c => new CategoryResponseDTO()
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    IsActive = c.IsActive,
                }).ToList();

                return new ApiResponse<List<CategoryResponseDTO>>(200, lstCategories);
            }
            catch (Exception ex)
            {
                // Log the exception (implementation depends on your logging setup)
                return new ApiResponse<List<CategoryResponseDTO>>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }
    }
}
