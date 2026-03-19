namespace SWD_Group4.BusinessLogic.DTO;

public sealed class SubmitVerificationRequestDto
{
    public string? Url { get; set; }

    public string CitizenId { get; set; } = string.Empty;

    public string BankAccount { get; set; } = string.Empty;

    public string BankName { get; set; } = string.Empty;

    public string CitizenImage { get; set; } = string.Empty;

    public string CitizenImageBack { get; set; } = string.Empty;

    public string BankCardImage { get; set; } = string.Empty;
}
