using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ECommerceApp.Models
{
    [Index(nameof(Name), Name = "IX_Name_Unique", IsUnique = true)]
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Category Name must be between 3 and 100 characters.")]
        public string Name { get; set; }

        [Required]
        [StringLength(500, ErrorMessage = "Description must be between 3 and 100 characters.")]
        public string Description { get; set; }
        public bool IsActive { get; set; }

        //Relations
        public ICollection<Product> Products { get; set; }
    }
}
