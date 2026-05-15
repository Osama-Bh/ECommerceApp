using ECommerceApp.Data;
using ECommerceApp.DTOs;
using ECommerceApp.DTOs.OrderDTOs;
using ECommerceApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace ECommerceApp.Services
{
    public interface IOrderService
    {
        Task<ApiResponse<OrderResponseDTO>> CreateOrderAsync(OrderCreateDTO2 orderCreateDTO, int customerId);

        Task<ApiResponse<OrderResponseDTO>> CreateOrderAsync(OrderCreateDTO orderDto);

        Task<ApiResponse<OrderResponseDTO>> GetOrderByIdAsync(int orderId);

        Task<ApiResponse<ConfirmationResponseDTO>> UpdateOrderStatusAsync(OrderStatusUpdateDTO statusDto);

        Task<ApiResponse<List<OrderResponseDTO>>> GetAllOrdersAsync();

        Task<ApiResponse<List<OrderResponseDTO>>> GetOrdersByCustomerIdAsync(int customerId);
    }

    public class OrderService : IOrderService
    {
        private readonly AppDbContext _context;

        // Allowed order status transitions for validating status changes.
        private static readonly Dictionary<OrderStatus, List<OrderStatus>> AllowedStatusTransitions = new()
        {
        { OrderStatus.Pending, new List<OrderStatus> { OrderStatus.Processing, OrderStatus.Canceled } },
        { OrderStatus.Processing, new List<OrderStatus> { OrderStatus.Shipped, OrderStatus.Canceled } },
        { OrderStatus.Shipped, new List<OrderStatus> { OrderStatus.Delivered } },
        { OrderStatus.Delivered, new List<OrderStatus>() }, // Terminal state
        { OrderStatus.Canceled, new List<OrderStatus>() }   // Terminal state
        };

        public OrderService(AppDbContext ctx)
        {
            _context = ctx;
        }

        public async Task<ApiResponse<OrderResponseDTO>> CreateOrderAsync(OrderCreateDTO2 orderCreateDTO, int customerId)
        {
            try
            {
                //1.Retrieve active cart for customer;
                var cart = await  _context.TbCarts.Include(c => c.CartItems).ThenInclude(ci => ci.Product).
                    FirstOrDefaultAsync(c => c.CustomerId == customerId && !c.IsCheckedOut);

                //check if cart equal null
                if (cart == null)
                    return new ApiResponse<OrderResponseDTO>(404, "There is no active cart for this customer");

                if (cart.CartItems.Count == 0)
                    return new ApiResponse<OrderResponseDTO>(404, "There is no items in your cart, cart is empty");

                //2.Retrieve Addresses and check if these Addresses belong to the customer
                var billingAddress = await _context.TbAddresses.FirstOrDefaultAsync(b => b.Id == orderCreateDTO.BillingAddressId);

                if (billingAddress == null || billingAddress.CustomerId != customerId)
                    return new ApiResponse<OrderResponseDTO>(400, "Billing Address is invalid or does not belong to the customer.");

                var shippingAddress = await _context.TbAddresses.FirstOrDefaultAsync(b => b.Id == orderCreateDTO.ShippingAddressId);

                if (shippingAddress == null || shippingAddress.CustomerId != customerId)
                    return new ApiResponse<OrderResponseDTO>(400, "Shipping Address is invalid or does not belong to the customer.");

                // Generate a unique order number.
                string orderNumber = GenerateOrderNumber();

                // List to hold order items.
                var orderItems = new List<OrderItem>();

                foreach (var item in cart.CartItems)
                {
                    //Check if sufficient stock is available.
                    if (item.Quantity > item.Product?.StockQuantity)
                    {
                        return new ApiResponse<OrderResponseDTO>(400, $"Insufficient stock for product {item.Product?.Name}.");
                    }

                    var orderItem = new OrderItem()
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Discount = item.Discout,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice,
                    };

                    //Add order items to list
                    orderItems.Add(orderItem);

                    // Deduct the purchased quantity from the product’s stock.
                    item.Product.StockQuantity -= item.Quantity;
                }

                // Initialize financial tracking.
                decimal totalBaseAmount = orderItems.Sum(oi => oi.UnitPrice * oi.Quantity);
                decimal totalDiscountAmount = orderItems.Sum(oi => oi.Discount * oi.Quantity);
                decimal totalAmount = orderItems.Sum(oi => oi.TotalPrice);

                decimal shippingCost = 10.00m; // Example fixed shipping cost.

                //Create Order object
                var order = new Order()
                {
                    OrderNumber = orderNumber,
                    CustomerId = customerId,
                    OrderDate = DateTime.UtcNow,
                    BillingAddressId = orderCreateDTO.BillingAddressId,
                    ShippingAddressId = orderCreateDTO.ShippingAddressId,
                    TotalBaseAmount = totalBaseAmount,
                    TotalDiscountAmount = totalDiscountAmount,
                    ShippingCost = shippingCost,
                    TotalAmount = totalAmount,
                    OrderStatus = OrderStatus.Pending,
                    OrderItems = orderItems
                };

                _context.TbOrders.Add(order);

                //update cart information
                cart.IsCheckedOut = true;
                cart.UpdatedAt = DateTime.UtcNow;

                //Save all changes
                await _context.SaveChangesAsync();

                //Map order to Order Response
                var orderResponseDto = MapOrderToDTO(order, orderItems);

                //Return response
                return new ApiResponse<OrderResponseDTO>(201, orderResponseDto);

            }
            catch (Exception ex)
            {
                return new ApiResponse<OrderResponseDTO>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<OrderResponseDTO>> CreateOrderAsync(OrderCreateDTO orderDto)
        {
            try
            {
                // Validate that the billing address exists and belongs to the customer.
                var billingAddress = await _context.TbAddresses.FindAsync(orderDto.BillingAddressId);
                if (billingAddress == null || billingAddress.CustomerId != orderDto.CustomerId)
                {
                    return new ApiResponse<OrderResponseDTO>(400, "Billing Address is invalid or does not belong to the customer.");
                }
                // Validate that the shipping address exists and belongs to the customer.
                var shippingAddress = await _context.TbAddresses.FindAsync(orderDto.ShippingAddressId);
                if (shippingAddress == null || shippingAddress.CustomerId != orderDto.CustomerId)
                {
                    return new ApiResponse<OrderResponseDTO>(400, "Shipping Address is invalid or does not belong to the customer.");
                }
                // Initialize financial tracking.
                decimal totalBaseAmount = 0;
                decimal totalDiscountAmount = 0;
                decimal shippingCost = 10.00m; // Example fixed shipping cost.
                decimal totalAmount = 0;
                // Generate a unique order number.
                string orderNumber = GenerateOrderNumber();
                // List to hold order items.
                var orderItems = new List<OrderItem>();
                // Process each order item from the DTO.
                foreach (var itemDto in orderDto.OrderItems)
                {
                    // Check if the product exists.
                    var product = await _context.TbProducts.FindAsync(itemDto.ProductId);
                    if (product == null)
                    {
                        return new ApiResponse<OrderResponseDTO>(404, $"Product with ID {itemDto.ProductId} does not exist.");
                    }
                    // Check if sufficient stock is available.
                    if (product.StockQuantity < itemDto.Quantity)
                    {
                        return new ApiResponse<OrderResponseDTO>(400, $"Insufficient stock for product {product.Name}.");
                    }
                    // Calculate base price, discount, and total price for the order item.
                    decimal basePrice = itemDto.Quantity * product.Price;
                    decimal discount = (product.DiscountPercentage / 100.0m) * basePrice;
                    decimal totalPrice = basePrice - discount;
                    // Create a new OrderItem.
                    var orderItem = new OrderItem
                    {
                        ProductId = product.Id,
                        Quantity = itemDto.Quantity,
                        UnitPrice = product.Price,
                        Discount = discount,
                        TotalPrice = totalPrice
                    };
                    // Add the order item to the list.
                    orderItems.Add(orderItem);
                    // Update the running totals.
                    totalBaseAmount += basePrice;
                    totalDiscountAmount += discount;
                    // Deduct the purchased quantity from the product’s stock.
                    product.StockQuantity -= itemDto.Quantity;
                    _context.TbProducts.Update(product);
                }
                // Calculate the final total amount.
                totalAmount = totalBaseAmount - totalDiscountAmount + shippingCost;
                // Manually map from DTO to Order model.
                var order = new Order
                {
                    OrderNumber = orderNumber,
                    CustomerId = orderDto.CustomerId,
                    OrderDate = DateTime.UtcNow,
                    BillingAddressId = orderDto.BillingAddressId,
                    ShippingAddressId = orderDto.ShippingAddressId,
                    TotalBaseAmount = totalBaseAmount,
                    TotalDiscountAmount = totalDiscountAmount,
                    ShippingCost = shippingCost,
                    TotalAmount = totalAmount,
                    OrderStatus = OrderStatus.Pending,
                    OrderItems = orderItems
                };
                // Add the order to the database.
                _context.TbOrders.Add(order);
                // Mark the customer's active cart as checked out (if it exists).
                var cart = await _context.TbCarts.FirstOrDefaultAsync(c => c.CustomerId == orderDto.CustomerId && !c.IsCheckedOut);
                if (cart != null)
                {
                    cart.IsCheckedOut = true;
                    cart.UpdatedAt = DateTime.UtcNow;
                    _context.TbCarts.Update(cart);
                }
                // Save all changes.
                await _context.SaveChangesAsync();
                // Map the saved order to OrderResponseDTO.
                var orderResponse = MapOrderToDTO(order, order.OrderItems);
                return new ApiResponse<OrderResponseDTO>(200, orderResponse);
            }
            catch (Exception ex)
            {
                return new ApiResponse<OrderResponseDTO>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<OrderResponseDTO>> GetOrderByIdAsync(int orderId)
        {
            try
            {
                // Retrieve the order with its items, customer, and addresses details.
                var order = await _context.TbOrders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.Customer)
                .Include(o => o.BillingAddress)
                .Include(o => o.ShippingAddress)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                {
                    return new ApiResponse<OrderResponseDTO>(404, "Order not found.");
                }

                // Map the order to a DTO.
                var orderResponse = MapOrderToDTO(order, order.OrderItems);
                return new ApiResponse<OrderResponseDTO>(200, orderResponse);
            }
            catch (Exception ex)
            {
                return new ApiResponse<OrderResponseDTO>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<ConfirmationResponseDTO>> UpdateOrderStatusAsync(OrderStatusUpdateDTO statusDto)
        {
            try
            {
                var order = await _context.TbOrders.FindAsync(statusDto.OrderId);

                if (order == null)
                    return new ApiResponse<ConfirmationResponseDTO>(404, "Order not found");

                OrderStatus currentStatus = order.OrderStatus;
                OrderStatus newStatus = statusDto.OrderStatus;

                // Validate the status transition.
                if (!AllowedStatusTransitions.TryGetValue(currentStatus, out var allowedStatuses))
                {
                    return new ApiResponse<ConfirmationResponseDTO>(500, "Current order status is invalid.");
                }
                if (!allowedStatuses.Contains(newStatus))
                {
                    return new ApiResponse<ConfirmationResponseDTO>(400, $"Cannot change order status from {currentStatus} to {newStatus}.");
                }

                //Update Order status
                order.OrderStatus = newStatus;
                await _context.SaveChangesAsync();

                //Prepar Response
                var response = new ConfirmationResponseDTO()
                {
                    Message = $"Order status with Id {order.Id} updated successfully"
                };

                return new ApiResponse<ConfirmationResponseDTO>(200, response);
            }
            catch (Exception ex)
            {
                return new ApiResponse<ConfirmationResponseDTO>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<OrderResponseDTO>>> GetAllOrdersAsync()
        {
            try
            {
                // Retrieve all orders with related entities.
                var orders = await _context.TbOrders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.Customer)
                .Include(o => o.BillingAddress)
                .Include(o => o.ShippingAddress)
                .AsNoTracking()
                .ToListAsync();

                var lstorderDTO = orders.Select(o => MapOrderToDTO(o, o.OrderItems)).ToList();

                return new ApiResponse<List<OrderResponseDTO>>(200, lstorderDTO);
            }
            catch (Exception ex)
            { 
                return new ApiResponse<List<OrderResponseDTO>>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<OrderResponseDTO>>> GetOrdersByCustomerIdAsync(int customerId)
        {
            try
            {
                var orders = await _context.TbOrders.Where(o => o.CustomerId == customerId)
               .Include(o => o.OrderItems)
               .ThenInclude(oi => oi.Product)
               .Include(o => o.Customer)
               .Include(o => o.BillingAddress)
               .Include(o => o.ShippingAddress)
               .AsNoTracking()
               .ToListAsync();

                var lstorderDTO = orders.Select(o => MapOrderToDTO(o, o.OrderItems)).ToList();

                return new ApiResponse<List<OrderResponseDTO>>(200, lstorderDTO);
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<OrderResponseDTO>>(500, $"An unexpected error occurred while processing your request, Error: {ex.Message}");
            }
        }

        private OrderResponseDTO MapOrderToDTO(Order order, ICollection<OrderItem> orderItems)
        {
            //convert orderItems to orderItemsDTO

            var orderItemsDTO = orderItems.Select(oi => new OrderItemResponseDTO()
            {
                Id = oi.Id,
                ProductId = oi.ProductId,
                Quantity = oi.Quantity,
                Discount = oi.Discount,
                UnitPrice = oi.UnitPrice,
                TotalPrice = oi.TotalPrice,
            }).ToList();

            // Create and return the DTO.
            return new OrderResponseDTO
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                OrderDate = order.OrderDate,
                CustomerId = order.CustomerId,
                BillingAddressId = order.BillingAddressId,
                ShippingAddressId = order.ShippingAddressId,
                TotalBaseAmount = order.TotalBaseAmount,
                TotalDiscountAmount = order.TotalDiscountAmount,
                ShippingCost = order.ShippingCost,
                TotalAmount = Math.Round(order.TotalAmount, 2),
                OrderStatus = order.OrderStatus.ToString(),
                OrderItems = orderItemsDTO
            };
        }

        private string GenerateOrderNumber()
        {
            return $"ORD-{DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")}-{RandomNumber(1000, 9999)}";
        }
        // Generates a random number between min and max.
        private int RandomNumber(int min, int max)
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var bytes = new byte[4];
                rng.GetBytes(bytes);
                return Math.Abs(BitConverter.ToInt32(bytes, 0) % (max - min + 1)) + min;
            }
        }
    }
}
