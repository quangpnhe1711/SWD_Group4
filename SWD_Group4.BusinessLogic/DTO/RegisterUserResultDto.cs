namespace SWD_Group4.BusinessLogic.DTO;

public sealed class RegisterUserResultDto
{
    public bool IsSuccess { get; init; }

    public string Message { get; init; } = string.Empty;

    public int? UserId { get; init; }
}
