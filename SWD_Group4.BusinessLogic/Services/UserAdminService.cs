using Microsoft.EntityFrameworkCore;
using SWD_Group4.BusinessLogic.DTO;
using SWD_Group4.BusinessLogic.IServices;
using SWD_Group4.DataAccess.Context;

namespace SWD_Group4.BusinessLogic.Services;

public sealed class UserAdminService : IUserAdminService
{
    private readonly BookStoreContext _dbContext;

    public UserAdminService(BookStoreContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<SellerListItemDto>> getSellerList()
    {
        // Auto-reactivate expired suspensions (demo scheduling)
        await _dbContext.Users
            .Where(u => u.Status == "Suspended" && u.SuspensionEndAt != null && u.SuspensionEndAt <= DateTime.Now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.Status, "Active")
                .SetProperty(u => u.SuspensionEndAt, (DateTime?)null));

        // "Seller" in this project is represented by Users with Role=Seller OR any KYC fields filled.
        return await _dbContext.Users
            .AsNoTracking()
            .Where(u => (u.Role != null && u.Role == "Seller")
                        || u.Url != null
                        || u.CitizenId != null
                        || u.BankAccount != null
                        || u.BankName != null
                        || u.CitizenImage != null
                        || u.CitizenImageBack != null)
            .Select(u => new SellerListItemDto
            {
                UserId = u.Id,
                Name = u.Name,
                Email = u.Email,
                Role = u.Role,
                Status = u.Status,
                Url = u.Url,
                CitizenId = u.CitizenId,
                BankAccount = u.BankAccount,
                BankName = u.BankName,
                SuspensionEndAt = u.SuspensionEndAt,
                LatestRequestId = u.VerificationRequests
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => (int?)r.Id)
                    .FirstOrDefault(),
                LatestRequestStatus = u.VerificationRequests
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => r.Status)
                    .FirstOrDefault(),
                LatestRequestCreatedAt = u.VerificationRequests
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => (DateTime?)r.CreatedAt)
                    .FirstOrDefault()
            })
            .OrderByDescending(x => x.LatestRequestCreatedAt)
            .ThenBy(x => x.UserId)
            .ToListAsync();
    }
}
