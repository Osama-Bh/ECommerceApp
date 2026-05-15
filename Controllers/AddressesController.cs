using ECommerceApp.DTOs;
using ECommerceApp.DTOs.AddressesDTOs;
using ECommerceApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ECommerceApp.Controllers
{
    [Authorize(Roles = "Admin, Customer")]
    [Route("api/[controller]")]
    [ApiController]
    public class AddressesController : ControllerBase
    {
        private readonly IAddressService _addressService;
        public AddressesController(IAddressService addressService)
        {
            _addressService = addressService;
        }

        [HttpPost("CreateAddress")]
        public async Task<ActionResult<ApiResponse<AddressResponseDTO>>> CreateAddress([FromBody]AddressCreateDTO addressCreateDTO)
        {
            var loggedInCustomerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            // Validate that CustomerId in body matches logged-in user
            if (addressCreateDTO.CustomerId != loggedInCustomerId)
                return Unauthorized("CustomerId mismatch. You can only act on your own account.");

            var response = await _addressService.CreateAddressAsync(addressCreateDTO);

            if (response.StatusCode != 201)
            {
                return StatusCode(response.StatusCode, response);
            }

            return Ok(response);
        }

        [HttpGet("GetAddressById/{AddressId}")]
        public async Task<ActionResult<ApiResponse<AddressResponseDTO>>> GetAddressById(int AddressId)
        {
            //var loggedInCustomerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            //// Validate that CustomerId in body matches logged-in user
            //if (AddressId != loggedInCustomerId)
            //    return Unauthorized("CustomerId mismatch. You can only act on your own account.");

            var response = await _addressService.GetAddressByIdAsync(AddressId);

            if (response.StatusCode != 200)
            {
                return StatusCode(response.StatusCode, response);
            }
            return Ok(response);
        }

        [HttpPut("UpdateAddress")]
        public async Task<ActionResult<ApiResponse<ConfirmationResponseDTO>>> UpdateAddress([FromBody]AddressUpdateDTO addressUpdateDTO)
        {
            var loggedInCustomerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            // Validate that CustomerId in body matches logged-in user
            if (addressUpdateDTO.CustomerId != loggedInCustomerId)
                return Unauthorized("CustomerId mismatch. You can only act on your own account.");

            var response = await _addressService.UpdateAddressAsync(addressUpdateDTO);

            if(response.StatusCode != 200)
            {
                return StatusCode(response.StatusCode, response);
            }
            return Ok(response);
        }

        [HttpDelete("DeleteAddress")]
        public async Task<ActionResult<ApiResponse<ConfirmationResponseDTO>>> DeleteAddress(AddressDeleteDTO addressDeleteDTO)
        {
            var loggedInCustomerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            // Validate that CustomerId in body matches logged-in user
            if (loggedInCustomerId == 0)
                return Unauthorized("CustomerId mismatch. You can only act on your own account.");

            var responsee = await _addressService.DeleteAddressAsync(addressDeleteDTO, loggedInCustomerId);

            if (responsee.StatusCode != 200)
                return StatusCode(responsee.StatusCode, responsee);

            return Ok(responsee);
        }

        [HttpGet("GetAddressesByCustomer/{customerId}")]
        public async Task<ActionResult<ApiResponse<ConfirmationResponseDTO>>> GetAddressByCustomer(int  customerId)
        {
            var loggedInCustomerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            // Validate that CustomerId in body matches logged-in user
            if (customerId != loggedInCustomerId)
                return Unauthorized("CustomerId mismatch. You can only act on your own account.");

            var response = await _addressService.GetAddressesByCustomerAsync(customerId);

            if (response.StatusCode != 200)
                return StatusCode(response.StatusCode, response);

            return Ok(response);
        }
    }
}
