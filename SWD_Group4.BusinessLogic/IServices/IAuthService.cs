using SWD_Group4.BusinessLogic.DTO;

namespace SWD_Group4.BusinessLogic.IServices;

public interface IAuthService
{
    Task<RegisterUserResultDto> registerUser(string name, string email, string password);

    Task<LoginResultDto> loginUser(string email, string password);
}
