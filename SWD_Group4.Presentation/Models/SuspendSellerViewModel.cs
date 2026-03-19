using System.ComponentModel.DataAnnotations;

namespace SWD_Group4.Presentation.Models;

public sealed class SuspendSellerViewModel : IValidatableObject
{
    [Required]
    public int UserId { get; set; }

    [Required]
    [MinLength(20, ErrorMessage = "Suspension reason must contain at least 20 characters.")]
    public string Reason { get; set; } = string.Empty;

    // OneWeek | OneMonth | OneYear | Permanent
    [Required]
    public string DurationType { get; set; } = "OneWeek";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var allowed = new[] { "OneWeek", "OneMonth", "OneYear", "Permanent" };
        if (!allowed.Any(a => string.Equals(a, DurationType, StringComparison.OrdinalIgnoreCase)))
        {
            yield return new ValidationResult("Invalid duration option.", new[] { nameof(DurationType) });
        }
    }
}
