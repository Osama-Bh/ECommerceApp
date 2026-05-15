using ECommerceApp.DTOs;
using ECommerceApp.DTOs.ProductDTOs;
using ECommerceApp.Models;
using ECommerceApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace ECommerceApp.Controllers
{
    [Authorize(Roles ="Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpPost("[action]")]
        public async Task<ActionResult<ApiResponse<ProductResponseDTO>>> CreateProduct([FromForm] ProductCreateDTO productCreateDTO)
        {
            var response = await _productService.CreateProductAsync(productCreateDTO);

            if (response.StatusCode != 201)
                return StatusCode(response.StatusCode, response);

            return Ok(response);
        }

        [HttpGet("[action]")]
        public async Task<ActionResult<ApiResponse<ProductResponseDTO>>> GetAllProducts()
        {
            var response = await _productService.GetAllProductsAsync();

            if (response.StatusCode != 200)
                return StatusCode(response.StatusCode, response);

            return Ok(response);

        }

        [HttpGet("[action]/{productId}")]
        public async Task<ActionResult<ApiResponse<ProductResponseDTO>>> GetProductById(int productId)
        {
            var response = await _productService.GetProductByIdAsync(productId);

            if (response.StatusCode != 200)
                return StatusCode(response.StatusCode, response);

            return Ok(response);

        }

        [HttpGet("ProductsByCategory/{categoryId}")]
        public async Task<ActionResult<ApiResponse<ProductResponseDTO>>> GetAllProductsByCategory(int categoryId)
        {
            var response = await _productService.GetAllProductsByCategoryAsync(categoryId);

            if (response.StatusCode != 200)
                return StatusCode(response.StatusCode, response);

            return Ok(response);

        }

        [HttpPut("UpdateProduct")]
        public async Task<ActionResult<ConfirmationResponseDTO>> UpdateProduct([FromForm]ProductUpdateDTO productUpdateDTO)
        {
            var response = await _productService.UpdateProductAsync(productUpdateDTO);

            if (response.StatusCode != 200)
                return StatusCode(response.StatusCode, response);

            return Ok(response);
        }

        [HttpPut("[action]")]
        public async Task<ActionResult<ConfirmationResponseDTO>> UpdateProductState(ProductStatusUpdateDTO productStatusUpdateDTO)
        {
            var response = await _productService.UpdateProductStatusAsync(productStatusUpdateDTO);

            if (response.StatusCode != 200)
                return StatusCode(response.StatusCode, response);

            return Ok(response);
        }

        [HttpDelete("[action]/{productId}")]
        public async Task<ActionResult<ConfirmationResponseDTO>> DeleteProduct(int productId)
        {
            var response = await _productService.DeleteProductAsync(productId);

            if (response.StatusCode != 200)
                return StatusCode(response.StatusCode, response);

            return Ok(response);
        }
    }
}
