namespace SWD_Group4.BusinessLogic.DTO;

public sealed class OrderDto
{
    public int OrderId { get; set; }

    public int CustomerId { get; set; }

    public List<OrderItemDto> OrderItems { get; set; } = new();

    public double TotalPrice { get; set; }

    public string OrderStatus { get; set; } = string.Empty;
}
