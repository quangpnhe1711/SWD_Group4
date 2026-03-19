using System.ComponentModel.DataAnnotations;

namespace SWD_Group4.Presentation.Models;

public sealed class RegisterUserViewModel
{
    [Required]
    [Display(Name = "Username")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare(nameof(Password), ErrorMessage = "Confirm Password does not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
