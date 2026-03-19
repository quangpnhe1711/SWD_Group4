using SWD_Group4.BusinessLogic.DTO;

namespace SWD_Group4.BusinessLogic.IServices;

public interface ISuspensionService
{
    // durationType: OneWeek | OneMonth | OneYear | Permanent
    Task<SuspendSellerResultDto> suspendUser(int userId, string reason, string durationType);

    Task<SuspendSellerResultDto> unsuspendUser(int userId);
}
