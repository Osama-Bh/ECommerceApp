using ECommerceApp.Data;
using ECommerceApp.DTOs;
using ECommerceApp.DTOs.UserDTOs;
using ECommerceApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly IAccountService _accoutService;

        // Injecting the services
        public AccountController(IAccountService accountService,
        IConfiguration config,
        UserManager<AppUser> userManager)
        {

            _configuration = config;
            _accoutService = accountService;
            _userManager = userManager;
        }
        // Registers a new customer.
        [HttpPost("RegisterUser")]
        public async Task<ActionResult<ApiResponse<UserResponseDTO>>> RegisterUser([FromBody] UserRegistrationDTO userDTO)
        {
            var response = await _accoutService.RegisterUserAsync(userDTO);
            if (response.StatusCode != 201)
            {
                return StatusCode((int)response.StatusCode, response);
            }
            return Ok(response);
        }

        // Logs in a customer.
        [HttpPost("Login")]
        public async Task<ActionResult<ApiResponse<LoginResponseDTO>>> Login([FromBody] LoginDTO loginDto)
        {
            var response = await _accoutService.LoginAsync(loginDto);
            if (response.StatusCode != 200)
            {
                return StatusCode((int)response.StatusCode, response);
            }
            return Ok(response);
        }

        [HttpPost("ConfirmEmail")]
        public async Task<ActionResult<ApiResponse<ConfirmationResponseDTO>>> ConfirmEmail([FromBody] EmailConfirmationDTO emailConfirmationDto)
        {
            if (emailConfirmationDto is null)
                    return BadRequest("Invalid email confirmation data.");

            var user = await _userManager.FindByEmailAsync(emailConfirmationDto.Email);
            if (user is null)
            {
                return NotFound("User not found.");
            }

            var isVerified = await _userManager.ConfirmEmailAsync(user, emailConfirmationDto.Code);

            if (isVerified.Succeeded)
            {
                return Ok(new ApiResponse<ConfirmationResponseDTO>(200, new ConfirmationResponseDTO
                {
                    Message = "Email confirmed successfully."
                }));
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<ConfirmationResponseDTO>(500, new ConfirmationResponseDTO
                {
                    Message = "Email confirmation failed."
                }));
            }
        }

        // Retrieves customer details by ID.
        [Authorize(Roles = "Customer")]
        [HttpGet("GetCustomerById/{id}")]
        public async Task<ActionResult<ApiResponse<UserResponseDTO>>> GetCustomerById(int id)
        {
            var response = await _accoutService.GetUserByIdAsync(id);
            if (response.StatusCode != 200)
            {
                return StatusCode((int)response.StatusCode, response);
            }
            return Ok(response);
        }

        // Updates customer details.
        [HttpPut("UpdateUser")]
        public async Task<ActionResult<ApiResponse<ConfirmationResponseDTO>>> UpdateCustomer([FromBody] UserUpdateDTO customerDto)
        {
            var response = await _accoutService.UpdateUserAsync(customerDto);
            if (response.StatusCode != 200)
            {
                return StatusCode((int)response.StatusCode, response);
            }
            return Ok(response);
        }

        // Deletes a customer by ID.
        [HttpDelete("DeleteUser/{id}")]
        public async Task<ActionResult<ApiResponse<ConfirmationResponseDTO>>> DeleteCustomer(int id)
        {
            var response = await _accoutService.DeleteUserAsync(id);
            if (response.StatusCode != 200)
            {
                return StatusCode((int)response.StatusCode, response);
            }
            return Ok(response);
        }

        // Changes the password for an existing customer.
        [HttpPost("ChangePassword")]
        public async Task<ActionResult<ApiResponse<ConfirmationResponseDTO>>> ChangePassword([FromBody] ChangePasswordDTO changePasswordDto)
        {
            var response = await _accoutService.ChangePasswordAsync(changePasswordDto);
            if (response.StatusCode != 200)
            {
                return StatusCode((int)response.StatusCode, response);
            }
            return Ok(response);
        }
    }
}
