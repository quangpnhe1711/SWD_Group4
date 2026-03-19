using SWD_Group4.BusinessLogic.DTO;

namespace SWD_Group4.BusinessLogic.IServices;

public interface IUserAdminService
{
    Task<List<SellerListItemDto>> getSellerList();
}
