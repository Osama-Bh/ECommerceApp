using System.ComponentModel.DataAnnotations;

namespace ECommerceApp.DTOs.OrderDTOs
{
    public class OrderCreateDTO2
    {
        [Required(ErrorMessage = "Billing Address Id is required")]
        public int BillingAddressId { get; set; }

        [Required(ErrorMessage = "Shipping Address is required")]
        public int ShippingAddressId { get; set; }
    }
}
