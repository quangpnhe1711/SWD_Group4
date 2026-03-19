namespace SWD_Group4.BusinessLogic.DTO;

public sealed class BookDto
{
    public int BookId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;

    public double Price { get; set; }
}