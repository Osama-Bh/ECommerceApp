using System.ComponentModel.DataAnnotations;

namespace ECommerceApp.Models
{
    public class Status
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }
    }
}
