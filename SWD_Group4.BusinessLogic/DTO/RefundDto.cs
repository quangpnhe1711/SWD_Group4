namespace SWD_Group4.BusinessLogic.DTO;

public sealed class RefundDto
{
    public int RefundId { get; set; }

    public int CustomerId { get; set; }

    public OrderDto? Order { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string RefundRequestStatus { get; set; } = string.Empty;
}
