using BCrypt.Net;
using ECommerceApp.Data;
using ECommerceApp.DTOs;
using ECommerceApp.DTOs.UserDTOs;
using ECommerceApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace ECommerceApp.Services
{
    public interface IAccountService
    {
        Task<ApiResponse<UserResponseDTO>> RegisterUserAsync(UserRegistrationDTO UserDTO);

        Task<ApiResponse<LoginResponseDTO>> LoginAsync(LoginDTO loginDTO);

        Task<ApiResponse<UserResponseDTO>> GetUserByIdAsync(int cusotmerId);

        Task<ApiResponse<ConfirmationResponseDTO>> UpdateUserAsync(UserUpdateDTO UserUpdateDto);

        Task<ApiResponse<ConfirmationResponseDTO>> DeleteUserAsync(int customerId);

        Task<ApiResponse<ConfirmationResponseDTO>> ChangePasswordAsync(ChangePasswordDTO changePasswordDto);
    }

    public class AccountService : IAccountService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public AccountService(AppDbContext context, UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager, IConfiguration configuration, IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _emailService = emailService;
        }

        public async Task<ApiResponse<UserResponseDTO>> RegisterUserAsync(UserRegistrationDTO UserDTO)
        {
            try
            {
                //check if email already exists
                if (await _userManager.FindByEmailAsync(UserDTO.Email) != null)
                    return new ApiResponse<UserResponseDTO>(200, "Email is already there");

                //Mapping data to the customer entity
                var newUser = new AppUser()
                {
                    FirstName = UserDTO.FirstName,
                    LastName = UserDTO.LastName,
                    UserName = UserDTO.Email,
                    Email = UserDTO.Email,
                    PhoneNumber = UserDTO.PhoneNumber,
                    DateOfBirth = UserDTO.DateOfBirth,
                    IsActive = true,
                };

                //add customer to database
                var result = await _userManager.CreateAsync(newUser, UserDTO.Password);

                if (!result.Succeeded)
                    return new ApiResponse<UserResponseDTO>(400, result.Errors.ToString());

                await _userManager.AddToRoleAsync(newUser, UserDTO.Role);

                var code = await _userManager.GenerateEmailConfirmationTokenAsync(newUser);

                var subject = "Welcome to My Store!";
                var body = $"<h1>Hello {newUser.FirstName},</h1><p>Thanks for registering at My Store you need to confirm your email to step " +
                    $"forward in the application the code {code}</p>";

                //await _emailService.SendEmailAsync(newUser.Email, subject, body);

                //prepar response DTO
                var UserResponseDto = new UserResponseDTO()
                {
                    Id = newUser.Id,
                    FirstName = newUser.FirstName,
                    LastName = newUser.LastName,
                    Email = newUser.Email,
                    PhoneNumber = newUser.PhoneNumber,
                    DateOfBirth = newUser.DateOfBirth,
                };

                return new ApiResponse<UserResponseDTO>(201, UserResponseDto);
            }
            catch (Exception ex)
            {
                // Log the exception (implementation depends on your logging setup)
                return new ApiResponse<UserResponseDTO>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<LoginResponseDTO>> LoginAsync(LoginDTO loginDTO)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(loginDTO.Email);

                //check email
                if (user == null)
                    return new ApiResponse<LoginResponseDTO>(400, "Invalid Email");

                //chek password
                //var result = await _userManager.CheckPasswordAsync(user, loginDTO.Password);
                if (!await _userManager.CheckPasswordAsync(user, loginDTO.Password))
                    return new ApiResponse<LoginResponseDTO>(400, "Invalid Password");

                string Token = GenerateJwtToken(user);
                //prepar response
                var loginResponseDto = new LoginResponseDTO()
                {
                    token = Token
                };

                return new ApiResponse<LoginResponseDTO>(200, loginResponseDto);
            }
            catch (Exception ex)
            {
                return new ApiResponse<LoginResponseDTO>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<UserResponseDTO>> GetUserByIdAsync(int userId)
        {
            try
            {

                var user = await _userManager.FindByIdAsync(userId.ToString());

                if (user == null || !user.IsActive)
                    return new ApiResponse<UserResponseDTO>(400, $"Customer with Id: [{userId}] Not found");

                var userResponseDto = new UserResponseDTO()
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    DateOfBirth = user.DateOfBirth,
                };

                return new ApiResponse<UserResponseDTO>(200, userResponseDto);
            }
            catch (Exception ex)
            {
                return new ApiResponse<UserResponseDTO>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<ConfirmationResponseDTO>> UpdateUserAsync(UserUpdateDTO userUpdateDto)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userUpdateDto.UserId.ToString());

                if (user == null || user.IsActive == false)
                    return new ApiResponse<ConfirmationResponseDTO>(404, "Customer not found");

                var exists = await _userManager.FindByEmailAsync(userUpdateDto.Email);
                if (user.Email != userUpdateDto.Email && exists != null)
                    return new ApiResponse<ConfirmationResponseDTO>(200, "Email already in use");

                user.DateOfBirth = userUpdateDto.DateOfBirth;
                user.FirstName = userUpdateDto.FirstName;
                user.LastName = userUpdateDto.LastName;
                user.Email = userUpdateDto.Email;
                user.PhoneNumber = userUpdateDto.PhoneNumber;

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    return new ApiResponse<ConfirmationResponseDTO>
                        (200, new ConfirmationResponseDTO() { Message = $"Customer {user.Id} updated successfully" });
                }
                else
                    return new ApiResponse<ConfirmationResponseDTO>(400, new ConfirmationResponseDTO { Message = $"Customer {user.Id} update fialed" });
                    
            }
            catch (Exception ex)
            {
                return new ApiResponse<ConfirmationResponseDTO>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<ConfirmationResponseDTO>> DeleteUserAsync(int  userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());

                if (user == null || !user.IsActive)
                    return new ApiResponse<ConfirmationResponseDTO>(404, $"Customer with Id: [{userId}] not found");

                user.IsActive = false;

                await _userManager.UpdateAsync(user);

                return new ApiResponse<ConfirmationResponseDTO>
                    (200, new ConfirmationResponseDTO() { Message = $"Customer wiht Id: [{userId}] deleted successfully" });
            }
            catch (Exception ex)
            {
                return new ApiResponse<ConfirmationResponseDTO>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<ConfirmationResponseDTO>> ChangePasswordAsync(ECommerceApp.DTOs.UserDTOs.ChangePasswordDTO changePasswordDto)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(changePasswordDto.UserId.ToString());

                if (user == null || !user.IsActive)
                    return new ApiResponse<ConfirmationResponseDTO>(404, "Customer not found");


                var isPasswordValid = await _userManager.CheckPasswordAsync(user, changePasswordDto.CurrentPassword);
                if (!isPasswordValid)
                    return new ApiResponse<ConfirmationResponseDTO>(401, "Current password is incorrect.");

                var result = await _userManager.ChangePasswordAsync(user, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);

                if (result.Succeeded)
                {
                    var confirmationDto = new ConfirmationResponseDTO()
                    {
                        Message = "Password changed successfully"
                    };

                    return new ApiResponse<ConfirmationResponseDTO>(200, confirmationDto);
                }
                else
                {
                    return new ApiResponse<ConfirmationResponseDTO>(401, result.Errors.ToString());
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<ConfirmationResponseDTO>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }

        private string GenerateJwtToken(AppUser user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email)
            };

            // Add user roles
            var roles = _userManager.GetRolesAsync(user).Result;
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:Issuer"],
                audience: _configuration["JWT:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(3),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

