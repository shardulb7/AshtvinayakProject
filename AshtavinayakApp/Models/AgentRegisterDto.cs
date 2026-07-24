using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace AshtavinayakAPP.Models
{
    public class AgentRegisterDto
    {
        [Required(ErrorMessage = "FullName is required.")]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "BusinessName is required.")]
        public string BusinessName { get; set; } = null!;

        [Required(ErrorMessage = "MobileNumber is required.")]
        public string MobileNumber { get; set; } = null!;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Email is not a valid email address.")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Address is required.")]
        public string Address { get; set; } = null!;

        // At least one letter and one digit, minimum 8 characters.
        [Required(ErrorMessage = "Password is required.")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).{8,}$",
            ErrorMessage = "Password must be at least 8 characters and include at least one letter and one digit.")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "ConfirmPassword is required.")]
        [Compare(nameof(Password), ErrorMessage = "Password and ConfirmPassword do not match.")]
        public string ConfirmPassword { get; set; } = null!;

        [Required(ErrorMessage = "Aadhaar Card document is required.")]
        public IFormFile AadhaarDocument { get; set; } = null!;

        [Required(ErrorMessage = "Shop Act License document is required.")]
        public IFormFile ShopActLicense { get; set; } = null!;

        [Required(ErrorMessage = "Udyam Registration Certificate document is required.")]
        public IFormFile UdyamCertificate { get; set; } = null!;
    }
}
