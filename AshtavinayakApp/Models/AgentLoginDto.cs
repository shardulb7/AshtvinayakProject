using System.ComponentModel.DataAnnotations;

namespace AshtavinayakAPP.Models
{
    public class AgentLoginDto
    {
        [Required(ErrorMessage = "MobileOrEmail is required.")]
        public string MobileOrEmail { get; set; } = null!;

        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; } = null!;
    }
}
