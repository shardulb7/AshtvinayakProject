using System.ComponentModel.DataAnnotations;

namespace AshtavinayakAPP.Models
{
    public class ResolveCustomerDto
    {
        [Required(ErrorMessage = "CustomerName is required.")]
        public string CustomerName { get; set; } = null!;

        [Required(ErrorMessage = "CustomerPhone is required.")]
        public string CustomerPhone { get; set; } = null!;

        public string? CustomerEmail { get; set; }
    }
}
