using ECommerceApp.DTOs;
using ECommerceApp.DTOs.OrderDTOs;
using ECommerceApp.Models;
using ECommerceApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ECommerceApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [Authorize(Roles ="Admin, Customer")]
        [HttpPost("CreateOrder")]
        public async Task<ActionResult<ApiResponse<OrderResponseDTO>>> CreateOrder(OrderCreateDTO2 orderCreateDTO2)
        {
            if (User.FindFirst(ClaimTypes.NameIdentifier) == null)
                return Unauthorized("You have to login first");

            var loggedInCustomerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            // Validate that CustomerId in body matches logged-in user
            if (loggedInCustomerId == 0)
                return Unauthorized("CustomerId mismatch. You can only act on your own account.");

            var response = await _orderService.CreateOrderAsync(orderCreateDTO2, loggedInCustomerId);
            if (response.StatusCode != 201)
                return StatusCode(response.StatusCode, response);

            return Ok(response);
        }

        [Authorize(Roles = "Admin, Customer")]
        [HttpGet("GetById/{orderId}")]
        public async Task<ActionResult<ApiResponse<OrderResponseDTO>>> GetOrderById(int orderId)
        {
            if (User.FindFirst(ClaimTypes.NameIdentifier) == null)
                return Unauthorized("You have to login first");

            var loggedInCustomerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            // Validate that CustomerId in body matches logged-in user
            if (loggedInCustomerId == 0)
                return Unauthorized("CustomerId mismatch. You can only act on your own account.");

            var response = await _orderService.GetOrderByIdAsync(orderId);
            if (response.StatusCode != 200)
                return StatusCode(response.StatusCode, response);

            return Ok(response);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<OrderResponseDTO>>>> GetAllOrders()
        {
            if (User.FindFirst(ClaimTypes.NameIdentifier) == null)
                return Unauthorized("You have to login first");

            var response = await _orderService.GetAllOrdersAsync();
            if (response.StatusCode != 200)
                return StatusCode(response.StatusCode, response);

            return Ok(response);
        }

        [Authorize(Roles = "Admin, Customer")]
        [HttpGet("ByCustomerId/{customerId}")]
        public async Task<ActionResult<ApiResponse<List<OrderResponseDTO>>>> GetAllOrdersByCustomerId(int customerId)
        {
            if (User.FindFirst(ClaimTypes.NameIdentifier) == null)
                return Unauthorized("You have to login first");

            var loggedInCustomerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            // Validate that CustomerId in body matches logged-in user
            //if (loggedInCustomerId != customerId)
            //    return Unauthorized("CustomerId mismatch. You can only act on your own account.");
            
            var response = await _orderService.GetOrdersByCustomerIdAsync(customerId);
            if (response.StatusCode != 200)
                return StatusCode(response.StatusCode, response);

            return Ok(response);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("UpdateOrder")]
        public async Task<ActionResult<ApiResponse<ConfirmationResponseDTO>>> UpdateOrderStatus(OrderStatusUpdateDTO orderStatusUpdateDTO)
        {
            if (User.FindFirst(ClaimTypes.NameIdentifier) == null)
                return Unauthorized("You have to login first");

            var loggedInCustomerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            // Validate that CustomerId in body matches logged-in user
            if (loggedInCustomerId == 0)
                return Unauthorized("CustomerId mismatch. You can only act on your own account.");

            var response = await _orderService.UpdateOrderStatusAsync(orderStatusUpdateDTO);
            if (response.StatusCode != 200)
                return StatusCode(response.StatusCode, response);

            return Ok(response);
        }
    }
}
