using System.ComponentModel.DataAnnotations;

namespace ABCRetailers.ViewModels
{
    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Required]
        [Display(Name = "Role")]
        public string Role { get; set; } // Add this property

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }
}