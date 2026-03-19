using System.ComponentModel.DataAnnotations;

namespace SWD_Group4.Presentation.Models;

public sealed class RejectVerificationRequestViewModel
{
    [Required]
    public int RequestId { get; set; }

    [Required]
    [MinLength(20, ErrorMessage = "Reason must be at least 20 characters.")]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;
}
