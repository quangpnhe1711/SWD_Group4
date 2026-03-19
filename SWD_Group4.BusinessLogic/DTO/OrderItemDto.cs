namespace SWD_Group4.BusinessLogic.DTO;

public sealed class OrderItemDto
{
    public int OrderItemId { get; set; }

    public int Quantity { get; set; }

    public double Price { get; set; }

    public BookDto? Book { get; set; }
}
