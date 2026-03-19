using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace SWD_Group4.Presentation.Models;

public sealed class SubmitVerificationRequestViewModel : IValidatableObject
{
    [StringLength(500)]
    [Display(Name = "URL")]
    [Url]
    public string? Url { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Citizen ID (CCCD)")]
    public string CitizenId { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    [Display(Name = "Bank Account")]
    public string BankAccount { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    [Display(Name = "Bank Name")]
    public string BankName { get; set; } = string.Empty;

    // Demo: allow upload OR manual URL.
    [StringLength(1000)]
    [Display(Name = "CCCD Front Image")]
    public string? CitizenImage { get; set; }

    [Display(Name = "CCCD Front Image File")]
    public IFormFile? CitizenImageFile { get; set; }

    [StringLength(1000)]
    [Display(Name = "CCCD Back Image")]
    public string? CitizenImageBack { get; set; }

    [Display(Name = "CCCD Back Image File")]
    public IFormFile? CitizenImageBackFile { get; set; }

    [StringLength(1000)]
    [Display(Name = "Bank Card Image")]
    public string? BankCardImage { get; set; }

    [Display(Name = "Bank Card Image File")]
    public IFormFile? BankCardImageFile { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CitizenImageFile == null && string.IsNullOrWhiteSpace(CitizenImage))
        {
            yield return new ValidationResult("CCCD front image is required.", new[] { nameof(CitizenImageFile), nameof(CitizenImage) });
        }

        if (CitizenImageBackFile == null && string.IsNullOrWhiteSpace(CitizenImageBack))
        {
            yield return new ValidationResult("CCCD back image is required.", new[] { nameof(CitizenImageBackFile), nameof(CitizenImageBack) });
        }

        if (BankCardImageFile == null && string.IsNullOrWhiteSpace(BankCardImage))
        {
            yield return new ValidationResult("Bank card image is required.", new[] { nameof(BankCardImageFile), nameof(BankCardImage) });
        }
    }
}
