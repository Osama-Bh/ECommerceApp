using ECommerceApp.DTOs;
using ECommerceApp.DTOs.AddressesDTOs;
using ECommerceApp.DTOs.ShoppingCartDTOs;
using ECommerceApp.Models;
using ECommerceApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ECommerceApp.Controllers
{
    [Authorize(Roles ="Customer")]
    [Route("api/[controller]")]
    [ApiController]
    public class CartsController : ControllerBase
    {
        private readonly IShoppingCartService _shoppingCartService;

        public CartsController(IShoppingCartService shoppingCartService)
        {
            _shoppingCartService = shoppingCartService;
        }

        [HttpGet("GetCart")]
        public async Task<ActionResult<ApiResponse<CartResponseDTO>>> GetCart()
        {
            var loggedInCustomerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            // Validate that CustomerId in body matches logged-in user
            if (loggedInCustomerId == 0)
                return Unauthorized("CustomerId mismatch. You can only act on your own account.");

            var response = await _shoppingCartService.GetCartByCustomerIdAsync(loggedInCustomerId);
            if (response.StatusCode != 200)
                return StatusCode(response.StatusCode, response);

            return Ok(response);
        }

        [HttpPost("AddToCart")]
        public async Task<ActionResult<ApiResponse<CartResponseDTO>>> AddToCart(AddToCartDTO addToCartDTO)
        {
            var loggedInCustomerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            // Validate that CustomerId in body matches logged-in user
            if (addToCartDTO.CustomerId != loggedInCustomerId)
                return Unauthorized("CustomerId mismatch. You can only act on your own account.");

            var response = await _shoppingCartService.AddToCartAsync(addToCartDTO);
            if (response.StatusCode != 201)
                return StatusCode(response.StatusCode, response);
            
            return Ok(response);
        }

        [HttpPut("UpdateCartItem")]
        public async Task<ActionResult<ApiResponse<CartResponseDTO>>> UpdateCartItem(UpdateCartItemDTO updateCartItemDTO)
        {
            var loggedInCustomerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            // Validate that CustomerId in body matches logged-in user
            if (updateCartItemDTO.CustomerId != loggedInCustomerId)
                return Unauthorized("CustomerId mismatch. You can only act on your own account.");

            var response = await _shoppingCartService.UpdateCartItemAsync(updateCartItemDTO);
            if (response.StatusCode != 200)
                return StatusCode(response.StatusCode, response);

            return Ok(response);
        }

        [HttpDelete("[action]")]
        public async Task<ActionResult<ApiResponse<CartResponseDTO>>> RemoveCartItem(RemoveCartItemDTO removeCartItemDTO)
        {
            var loggedInCustomerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            // Validate that CustomerId in body matches logged-in user
            if (removeCartItemDTO.CustomerId != loggedInCustomerId)
                return Unauthorized("CustomerId mismatch. You can only act on your own account.");

            var response = await _shoppingCartService.RemoveCartItemAsync(removeCartItemDTO);
            if (response.StatusCode != 200)
                return StatusCode(response.StatusCode, response);

            return Ok(response);
        }

        [HttpDelete("ClearCart")]
        public async Task<ActionResult<ConfirmationResponseDTO>> ClearCart()
        {
            var loggedInCustomerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            // Validate that CustomerId in body matches logged-in user
            if (loggedInCustomerId == 0)
                return Unauthorized("CustomerId mismatch. You can only act on your own account.");

            var response = await _shoppingCartService.ClearCartAsync(loggedInCustomerId);
            if (response.StatusCode != 200)
                return StatusCode(response.StatusCode, response);

            return Ok(response);
        }
    }
}
