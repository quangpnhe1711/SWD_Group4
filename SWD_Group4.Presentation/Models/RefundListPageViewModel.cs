using SWD_Group4.BusinessLogic.DTO;

namespace SWD_Group4.Presentation.Models;

public sealed class RefundListPageViewModel
{
    public int SellerId { get; set; }

    public string Message { get; set; } = string.Empty;

    public List<RefundDto> Refunds { get; set; } = new();
}
