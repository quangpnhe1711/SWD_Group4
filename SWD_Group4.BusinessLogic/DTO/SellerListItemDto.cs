namespace SWD_Group4.BusinessLogic.DTO;

public sealed class SellerListItemDto
{
    public int UserId { get; init; }

    public string? Name { get; init; }

    public string? Email { get; init; }

    public string? Role { get; init; }

    public string? Status { get; init; }

    public string? Url { get; init; }

    public string? CitizenId { get; init; }

    public string? BankAccount { get; init; }

    public string? BankName { get; init; }

    public DateTime? SuspensionEndAt { get; init; }

    public int? LatestRequestId { get; init; }

    public string? LatestRequestStatus { get; init; }

    public DateTime? LatestRequestCreatedAt { get; init; }
}
