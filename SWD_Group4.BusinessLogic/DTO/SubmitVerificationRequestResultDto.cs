namespace SWD_Group4.BusinessLogic.DTO;

public sealed class SubmitVerificationRequestResultDto
{
    public bool IsSuccess { get; set; }

    public int? RequestId { get; set; }

    public string Message { get; set; } = string.Empty;
}
