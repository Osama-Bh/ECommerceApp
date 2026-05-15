using ECommerceApp.Data;
using ECommerceApp.DTOs;
using ECommerceApp.DTOs.ShoppingCartDTOs;
using ECommerceApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ECommerceApp.Services
{
    public interface IShoppingCartService
    {
        Task<ApiResponse<CartResponseDTO>> GetCartByCustomerIdAsync(int customerId);
        Task<ApiResponse<CartResponseDTO>> AddToCartAsync(AddToCartDTO addToCartDTO);
        Task<ApiResponse<CartResponseDTO>> UpdateCartItemAsync(UpdateCartItemDTO updateCartItemDTO);
        Task<ApiResponse<CartResponseDTO>> RemoveCartItemAsync(RemoveCartItemDTO removeCartItemDTO);
        Task<ApiResponse<ConfirmationResponseDTO>> ClearCartAsync(int customerId);
    }

    public class ShoppingCartService : IShoppingCartService
    {
        private readonly AppDbContext _context;

        public ShoppingCartService(AppDbContext ctx)
        {
            _context = ctx;
        }

        public async Task<ApiResponse<CartResponseDTO>> GetCartByCustomerIdAsync(int customerId)
        {
            try
            {
                //check if the customerId == Loggedin User for security

                // Query the database for a cart that belongs to the specified customer and is not checked out.
                var cart = await _context.TbCarts
                .Include(c => c.CartItems) // Include the cart items in the query
                .ThenInclude(ci => ci.Product) // Also include the product details for each cart item
                .FirstOrDefaultAsync(c => c.CustomerId == customerId && !c.IsCheckedOut);

                // If no active cart is found, create an empty DTO with default values.
                if (cart == null)
                {
                    var emptyCartDto = new CartResponseDTO()
                    {
                        CustomerId = customerId,
                        IsCheckedOut = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        CartItems = new List<CartItemResponseDTO>(),
                        TotalBasePrice = 0,
                        TotalDiscount = 0,
                        TotalAmount = 0
                    };
                    // Return the empty cart wrapped in an ApiResponse with status code 200 (OK).
                    return new ApiResponse<CartResponseDTO>(200, emptyCartDto);
                }

                var cartDto = MapCartToDTO(cart);
                return new ApiResponse<CartResponseDTO>(200, cartDto);
            }
            catch (Exception ex)
            {
                // In case of an exception, return a 500 status code with an error message.
                return new ApiResponse<CartResponseDTO>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<CartResponseDTO>> AddToCartAsync(AddToCartDTO addToCartDTO)
        {
            try
            {
                // 1. Retrieve the product
                var product = await _context.TbProducts.FindAsync(addToCartDTO.ProductId);
                if (product == null)
                    return new ApiResponse<CartResponseDTO>(404, "Product not found.");

                // 2. Check stock
                if (addToCartDTO.Quantity > product.StockQuantity)
                    return new ApiResponse<CartResponseDTO>(400, $"Only {product.StockQuantity} units of {product.Name} are available.");

                // 3. Find active cart
                var cart = await _context.TbCarts
                    .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.CustomerId == addToCartDTO.CustomerId && !c.IsCheckedOut);

                // 4. If no cart exists → create one
                if (cart == null)
                {
                    cart = new Cart
                    {
                        CustomerId = addToCartDTO.CustomerId,
                        IsCheckedOut = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        CartItems = new List<CartItem>()
                    };

                    _context.TbCarts.Add(cart);   // <-- NEW entity → Add
                    await _context.SaveChangesAsync(); // save immediately so cart.Id gets generated
                }

                // 5. Check if product already exists in cart
                var existingCartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == addToCartDTO.ProductId);

                if (existingCartItem != null)
                {
                    // If new total exceeds stock
                    if (existingCartItem.Quantity + addToCartDTO.Quantity > product.StockQuantity)
                        return new ApiResponse<CartResponseDTO>(400, $"Adding {addToCartDTO.Quantity} exceeds available stock.");

                    // Update tracked entity (no Update() needed)
                    existingCartItem.Quantity += addToCartDTO.Quantity;
                    existingCartItem.TotalPrice = (existingCartItem.UnitPrice - existingCartItem.Discout) * existingCartItem.Quantity;
                    existingCartItem.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // Create new cart item
                    var discount = product.DiscountPercentage > 0 ? product.Price * product.DiscountPercentage / 100 : 0;

                    var cartItem = new CartItem
                    {
                        CartId = cart.Id,
                        ProductId = product.Id,
                        Quantity = addToCartDTO.Quantity,
                        UnitPrice = product.Price,
                        Discout = discount,
                        TotalPrice = (product.Price - discount) * addToCartDTO.Quantity,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.TbCartItems.Add(cartItem);  // <-- NEW entity → Add
                }

                // 6. Update cart timestamp (no Update() needed)
                cart.UpdatedAt = DateTime.UtcNow;

                // 7. Save changes (handles all tracked updates and new inserts)
                await _context.SaveChangesAsync();

                // 8. Reload cart with latest details
                cart = await _context.TbCarts
                    .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.Id == cart.Id) ?? new Cart();

                var cartDTO = MapCartToDTO(cart);
                return new ApiResponse<CartResponseDTO>(201, cartDTO);
            }
            catch (Exception ex)
            {
                return new ApiResponse<CartResponseDTO>(500, $"An unexpected error occurred: {ex.Message}");
            }
        }

        public async Task<ApiResponse<CartResponseDTO>> UpdateCartItemAsync(UpdateCartItemDTO updateCartItemDTO)
        {
            try
            {
                //Retrieve cart
                var cart = await _context.TbCarts.Include(c => c.CartItems).ThenInclude(ci => ci.Product).
                    FirstOrDefaultAsync(c => c.CustomerId == updateCartItemDTO.CustomerId && !c.IsCheckedOut);
                
                //check if car is null
                if (cart == null)
                    return new ApiResponse<CartResponseDTO>(404, "Active cart not found.");

                //Retrieve cartItem
                var cartItem = cart.CartItems?.FirstOrDefault(ci => ci.Id == updateCartItemDTO.CartItemId);
                
                //check if the new quantity will exceed the stock quantity
                if (updateCartItemDTO.Quantity > cartItem.Product.StockQuantity)
                    return new ApiResponse<CartResponseDTO>(400, $"Only {cartItem.Product.StockQuantity} units of {cartItem.Product.Name} are available.");

                //update cartItem
                cartItem.Quantity = updateCartItemDTO.Quantity;
                cartItem.TotalPrice = (cartItem.UnitPrice - cartItem.Discout) * cartItem.Quantity;
                cartItem.UpdatedAt = DateTime.UtcNow;

                //update cart updateAt date
                cart.UpdatedAt = DateTime.UtcNow;

                //save changes
                await _context.SaveChangesAsync();

                //Reload cart with new changes
                cart = await _context.TbCarts.Include(c => c.CartItems).ThenInclude(ci => ci.Product).
                    FirstOrDefaultAsync(c => c.CustomerId == updateCartItemDTO.CustomerId && !c.IsCheckedOut);

                //Map cart to DTO
                var carResponseDto = MapCartToDTO(cart);

                return new ApiResponse<CartResponseDTO>(200, carResponseDto);
            }
            catch (Exception ex)
            {
                return new ApiResponse<CartResponseDTO>(500, $"An unexpected error occurred: {ex.Message}");
            }
        }

        public async Task<ApiResponse<CartResponseDTO>> RemoveCartItemAsync(RemoveCartItemDTO removeCartItemDTO)
        {
            try
            {
                //Retrieve cart
                var cart = await _context.TbCarts.Include(c => c.CartItems).ThenInclude(ci => ci.Product).
                    FirstOrDefaultAsync(c => c.CustomerId == removeCartItemDTO.CustomerId && !c.IsCheckedOut);

                if (cart == null)
                    return new ApiResponse<CartResponseDTO>(404, "Active cart not found");

                //Get cart Item
                var carItem = cart.CartItems.FirstOrDefault(ci => ci.Id == removeCartItemDTO.CartItemId);

                if (carItem == null)
                    return new ApiResponse<CartResponseDTO>(404, "Cart Item not foun");

                // Remove cart item from data base
                _context.TbCartItems.Remove(carItem);

                //update car UpdatedAt because cartItems List update when we remove cart item
                cart.UpdatedAt = DateTime.UtcNow;
                
                //save changes in data base
                await _context.SaveChangesAsync();

                //Reload cart again 
                cart = await _context.TbCarts.Include(c => c.CartItems).ThenInclude(ci => ci.Product).
                    FirstOrDefaultAsync(c => c.CustomerId == removeCartItemDTO.CustomerId && !c.IsCheckedOut);

                //Map cart to DTO
                var cartResponseDto = MapCartToDTO(cart);

                return new ApiResponse<CartResponseDTO>(200, cartResponseDto);
            }
            catch (Exception ex)
            {
                return new ApiResponse<CartResponseDTO>(500, $"An unexpected error occurred: {ex.Message}");
            }
        }

        // Clears all items from the customer's active cart.
        public async Task<ApiResponse<ConfirmationResponseDTO>> ClearCartAsync(int customerId)
        {
            try
            {
                // Retrieve the active cart along with its items.
                var cart = await _context.TbCarts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId && !c.IsCheckedOut);
                // Return 404 if no active cart is found.
                if (cart == null)
                {
                    return new ApiResponse<ConfirmationResponseDTO>(404, "Active cart not found.");
                }
                // If there are any items in the cart, remove them.
                if (cart.CartItems.Any())
                {
                    _context.TbCartItems.RemoveRange(cart.CartItems);
                    cart.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
                // Create a confirmation response DTO.
                var confirmation = new ConfirmationResponseDTO
                {
                    Message = "Cart has been cleared successfully."
                };
                return new ApiResponse<ConfirmationResponseDTO>(200, confirmation);
            }
            catch (Exception ex)
            {
                // Return error response if an exception occurs.
                return new ApiResponse<ConfirmationResponseDTO>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }

        private CartResponseDTO MapCartToDTO(Cart  cart)
        {
            // Map each CartItem entity to its corresponding CartItemResponseDTO.
            var cartItemsDTO = cart.CartItems?.Select(cr => new CartItemResponseDTO()
            {
                Id = cr.Id,
                ProductId = cr.ProductId,
                ProductName = cr.Product?.Name,
                Quantity = cr.Quantity,
                UnitPrice = cr.UnitPrice,
                Discount = cr.Discout,
                TotalPrice = cr.TotalPrice,
            }).ToList() ?? new List<CartItemResponseDTO>();

            //calculate sums
            decimal totalBasePrice = cartItemsDTO.Sum(ci => ci.UnitPrice * ci.Quantity);
            decimal totalDiscount = cartItemsDTO.Sum(ci => ci.Discount * ci.Quantity);
            decimal totalAmount = cartItemsDTO.Sum(ci => ci.TotalPrice);

            // Create and return the final CartResponseDTO with all details and calculated totals.
            var carResponseDto = new CartResponseDTO()
            {
                Id = cart.Id,
                CustomerId = cart.CustomerId,
                IsCheckedOut = cart.IsCheckedOut,
                CreatedAt = cart.CreatedAt,
                UpdatedAt = cart.UpdatedAt,
                CartItems = cartItemsDTO,
                TotalBasePrice = totalBasePrice,
                TotalDiscount = totalDiscount,
                TotalAmount = totalAmount
            };

            return carResponseDto;
        }
    }
}
