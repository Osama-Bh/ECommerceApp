using ECommerceApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ECommerceApp.Data
{
    //4[Index(nameof(Email), Name = "IX_Email_Unique", IsUnique = true)]
    public class AppUser : IdentityUser<int>
    {
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last Name is required")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Last Name must be between 2 and 50 characters.")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Date of birth is required")]
        public DateTime DateOfBirth { get; set; }

        public bool IsActive { get; set; }

        //Relations
        public ICollection<Address> Addresses { get; set; }
        public ICollection<Order> Orders { get; set; }
        // Navigation property: A user can have many carts but only 1 active cart
        public ICollection<Cart> Carts { get; set; }

        //public ICollection<Feedback> Feedbacks { get; set; }
    }
}
