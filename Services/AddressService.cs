using ECommerceApp.Data;
using ECommerceApp.DTOs;
using ECommerceApp.DTOs.AddressesDTOs;
using ECommerceApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ECommerceApp.Services
{
    public interface IAddressService
    {
        Task<ApiResponse<AddressResponseDTO>> CreateAddressAsync(AddressCreateDTO addressCreateDTO);

        Task<ApiResponse<AddressResponseDTO>> GetAddressByIdAsync(int addressId);

        Task<ApiResponse<ConfirmationResponseDTO>> UpdateAddressAsync(AddressUpdateDTO addressDto);

        Task<ApiResponse<ConfirmationResponseDTO>> DeleteAddressAsync(AddressDeleteDTO addressDeleteDTO, int UserId);

        Task<ApiResponse<List<AddressResponseDTO>>> GetAddressesByCustomerAsync(int userId);
    }

    public class AddressService : IAddressService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public AddressService(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<ApiResponse<AddressResponseDTO>> CreateAddressAsync(AddressCreateDTO addressCreateDTO)
        {
            try
            {
                //check the user in controller
                var newAddress = new Address()
                {
                    CustomerId = addressCreateDTO.CustomerId,
                    AddressLine1 = addressCreateDTO.AddressLine1,
                    AddressLine2 = addressCreateDTO.AddressLine2,
                    State = addressCreateDTO.State,
                    City = addressCreateDTO.City,
                    PostalCode = addressCreateDTO.PostalCode,
                    Country = addressCreateDTO.Country,
                };

                _context.TbAddresses.Add(newAddress);
                await _context.SaveChangesAsync();

                var addressResponse = new AddressResponseDTO
                {
                    Id = newAddress.Id,
                    CustomerId = newAddress.CustomerId,
                    AddressLine1 = newAddress.AddressLine1,
                    AddressLine2 = newAddress.AddressLine2,
                    City = newAddress.City,
                    State = newAddress.State,
                    PostalCode = newAddress.PostalCode,
                    Country = newAddress.Country
                };

                return new ApiResponse<AddressResponseDTO>(201, addressResponse);
            }
            catch (Exception ex)
            {
                return new ApiResponse<AddressResponseDTO>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<AddressResponseDTO>> GetAddressByIdAsync(int addressId)
        {
            try
            {
                var address = await _context.TbAddresses.Where(a => a.Id == addressId).FirstOrDefaultAsync();

                if (address == null)
                    return new ApiResponse<AddressResponseDTO>(404, "Address not found");

                var addressResponse = new AddressResponseDTO
                {
                    Id = address.Id,
                    CustomerId = address.CustomerId,
                    AddressLine1 = address.AddressLine1,
                    AddressLine2 = address.AddressLine2,
                    City = address.City,
                    State = address.State,
                    PostalCode = address.PostalCode,
                    Country = address.Country
                };

                return new ApiResponse<AddressResponseDTO>(200, addressResponse);
            }
            catch (Exception ex)
            {
                return new ApiResponse<AddressResponseDTO>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<ConfirmationResponseDTO>> UpdateAddressAsync(AddressUpdateDTO addressDto)
        {
            try
            {
                //check customer in controller
                var address = await _context.TbAddresses.Where(a => a.Id == addressDto.AddressId && a.CustomerId == addressDto.CustomerId).FirstOrDefaultAsync();

                if (address == null)
                    return new ApiResponse<ConfirmationResponseDTO>(404, new ConfirmationResponseDTO() { Message = "Address not found" });

                address.AddressLine1 = addressDto.AddressLine1;
                address.AddressLine2 = addressDto.AddressLine2;
                address.City = addressDto.City;
                address.State = addressDto.State;
                address.PostalCode = addressDto.PostalCode;
                address.Country = addressDto.Country;

                await _context.SaveChangesAsync();

                // Prepare confirmation message
                var confirmationMessage = new ConfirmationResponseDTO
                {
                    Message = $"Address with Id {addressDto.AddressId} updated successfully."
                };
                return new ApiResponse<ConfirmationResponseDTO>(200, confirmationMessage);

            }
            catch (Exception ex)
            {
                return new ApiResponse<ConfirmationResponseDTO>(500, new ConfirmationResponseDTO() { Message = $"An unexpected error occurred while processing your request, Error: {ex.Message}" });
            }
        }

        public async Task<ApiResponse<ConfirmationResponseDTO>> DeleteAddressAsync(AddressDeleteDTO addressDeleteDTO
            , int UserId)
        {
            try
            {
                var address = await _context.TbAddresses.FirstOrDefaultAsync(
                    a => a.Id == addressDeleteDTO.AddressId && a.CustomerId == UserId);

                if (address == null)
                {
                    return new ApiResponse<ConfirmationResponseDTO>(404,
                        new ConfirmationResponseDTO() { Message = "Address not found" });
                }

                _context.TbAddresses.Remove(address);
                await _context.SaveChangesAsync();

                return new ApiResponse<ConfirmationResponseDTO>(200,
                    new ConfirmationResponseDTO() { Message = "Address Deleted Successfully" });

            }catch (Exception ex)
            {
                return new ApiResponse<ConfirmationResponseDTO>(500, new ConfirmationResponseDTO() { Message = $"An unexpected error occurred while processing your request, Error: {ex.Message}" });
            }
        }

        public async Task<ApiResponse<List<AddressResponseDTO>>> GetAddressesByCustomerAsync(int userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());

                await _context.Entry(user)
                .Collection(u => u.Addresses)
                .LoadAsync();

                var addresses = user.Addresses.Select(a => new AddressResponseDTO
                {
                    Id = a.Id,
                    CustomerId = a.CustomerId,
                    AddressLine1 = a.AddressLine1,
                    AddressLine2 = a.AddressLine2,
                    City = a.City,
                    State = a.State,
                    PostalCode = a.PostalCode,
                    Country = a.Country
                }).ToList();
                return new ApiResponse<List<AddressResponseDTO>>(200, addresses);
            }
            catch (Exception ex)
            {
                // Log the exception
                return new ApiResponse<List<AddressResponseDTO>>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }
    }
}