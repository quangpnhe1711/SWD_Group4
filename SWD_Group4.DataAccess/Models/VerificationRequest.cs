using System;

namespace SWD_Group4.DataAccess.Models;

public partial class VerificationRequest
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? Url { get; set; }

    public string? CitizenId { get; set; }

    public string? BankAccount { get; set; }

    public string? BankName { get; set; }

    public string? CitizenImage { get; set; }

    public string? CitizenImageBack { get; set; }

    public string? BankCardImage { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? Approved { get; set; }

    public string? Reason { get; set; }

    public virtual User User { get; set; } = null!;
}
