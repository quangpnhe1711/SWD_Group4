namespace SWD_Group4.BusinessLogic.DTO;

public sealed class LoginResultDto
{
    public bool IsSuccess { get; init; }

    public string Message { get; init; } = string.Empty;

    public int? UserId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;
}
