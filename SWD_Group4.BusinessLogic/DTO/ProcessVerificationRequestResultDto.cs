namespace SWD_Group4.BusinessLogic.DTO;

public sealed class ProcessVerificationRequestResultDto
{
    public bool IsSuccess { get; set; }

    public string Message { get; set; } = string.Empty;
}
