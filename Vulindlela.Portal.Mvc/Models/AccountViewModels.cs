using System.ComponentModel.DataAnnotations;

namespace Vulindlela.Portal.Mvc.Models
{
    public sealed class SignInViewModel
    {
        [Required]
        [Display(Name = "ID number")]
        public string Username { get; set; } = "";

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        public string? ReturnUrl { get; set; }
        public string? Error { get; set; }
    }

    public sealed class RegisterViewModel
    {
        [Required]
        [Display(Name = "South African ID number")]
        public string CitizenId { get; set; } = "";

        [Required]
        [Display(Name = "Full name")]
        public string DisplayName { get; set; } = "";

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; } = "";

        public string? Error { get; set; }
    }
}
