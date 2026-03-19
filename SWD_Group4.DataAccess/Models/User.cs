using System;
using System.Collections.Generic;

namespace SWD_Group4.DataAccess.Models;

public partial class User
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Email { get; set; }

    public string? Password { get; set; }

    public string? Role { get; set; }

    public string? Status { get; set; }

    // Seller info (optional; only filled after verification approval)
    public string? Url { get; set; }

    public string? CitizenId { get; set; }

    public string? BankAccount { get; set; }

    public string? BankName { get; set; }

    public string? CitizenImage { get; set; }

    public string? CitizenImageBack { get; set; }

    public DateTime? SuspensionEndAt { get; set; }

    public virtual ICollection<Book> Books { get; set; } = new List<Book>();

    public virtual ICollection<Order> OrderCustomers { get; set; } = new List<Order>();

    public virtual ICollection<Order> OrderSellers { get; set; } = new List<Order>();

    public virtual ICollection<Promotion> Promotions { get; set; } = new List<Promotion>();

    public virtual ICollection<VerificationRequest> VerificationRequests { get; set; } = new List<VerificationRequest>();
}
