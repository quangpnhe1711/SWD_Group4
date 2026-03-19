using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SWD_Group4.BusinessLogic.DTO;
using SWD_Group4.BusinessLogic.IServices;
using SWD_Group4.DataAccess.Context;

namespace SWD_Group4.BusinessLogic.Services;

public sealed class SuspensionService : ISuspensionService
{
    private readonly BookStoreContext _dbContext;
    private readonly INotificationService _notificationService;
    private readonly ILogger<SuspensionService> _logger;

    public SuspensionService(BookStoreContext dbContext, INotificationService notificationService, ILogger<SuspensionService> logger)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<SuspendSellerResultDto> suspendUser(int userId, string reason, string durationType)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 20)
        {
            return new SuspendSellerResultDto { IsSuccess = false, Message = "Suspension reason must contain at least 20 characters." };
        }

        if (string.IsNullOrWhiteSpace(durationType))
        {
            return new SuspendSellerResultDto { IsSuccess = false, Message = "Invalid duration option." };
        }

        var now = DateTime.Now;
        var isPermanent = string.Equals(durationType, "Permanent", StringComparison.OrdinalIgnoreCase);

        DateTime? until = durationType.Trim() switch
        {
            var s when string.Equals(s, "Permanent", StringComparison.OrdinalIgnoreCase) => null,
            var s when string.Equals(s, "OneWeek", StringComparison.OrdinalIgnoreCase) => now.AddDays(7),
            var s when string.Equals(s, "OneMonth", StringComparison.OrdinalIgnoreCase) => now.AddMonths(1),
            var s when string.Equals(s, "OneYear", StringComparison.OrdinalIgnoreCase) => now.AddYears(1),
            _ => DateTime.MinValue
        };

        if (!isPermanent && until == DateTime.MinValue)
        {
            return new SuspendSellerResultDto { IsSuccess = false, Message = "Invalid duration option." };
        }

        try
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return new SuspendSellerResultDto { IsSuccess = false, Message = "User not found." };
            }

            user.Status = "Suspended";
            user.SuspensionEndAt = until;

            // 1) Persist suspension first (must not fail because of Book table constraints / bulk update issues)
            await _dbContext.SaveChangesAsync();

            // 2) Best-effort product lockdown
            var booksLocked = true;
            try
            {
                await _dbContext.Books
                    .Where(b => b.SellerId == userId)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(b => b.Status, "Hidden"));
            }
            catch (Exception ex)
            {
                booksLocked = false;
                _logger.LogError(ex, "Failed to lock seller books (set Book.Status='Hidden') for userId={UserId}", userId);
            }

            _logger.LogInformation("User suspended: userId={UserId}, until={Until}, reason={Reason}, durationType={DurationType}, booksLocked={BooksLocked}", userId, until, reason, durationType, booksLocked);

            // Do not fail suspension just because email sending fails
            try
            {
                await _notificationService.notifySellerSuspended(userId, reason.Trim(), until);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send suspension email for userId={UserId}", userId);
            }

            var msg = isPermanent
                ? "Seller suspended permanently."
                : $"Seller suspended until {until:yyyy-MM-dd HH:mm}.";

            if (!booksLocked)
            {
                msg += " (Warning: could not hide seller books due to DB constraints.)";
            }

            return new SuspendSellerResultDto { IsSuccess = true, Message = msg };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Suspend failed: userId={UserId}, durationType={DurationType}", userId, durationType);
            var root = ex.GetBaseException().Message;
            return new SuspendSellerResultDto { IsSuccess = false, Message = $"System error: Unable to suspend user. Root: {root}" };
        }
    }

    public async Task<SuspendSellerResultDto> unsuspendUser(int userId)
    {
        try
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return new SuspendSellerResultDto { IsSuccess = false, Message = "User not found." };
            }

            if (user.Status != "Suspended")
            {
                return new SuspendSellerResultDto { IsSuccess = false, Message = "User is not suspended." };
            }

            user.Status = "Active";
            user.SuspensionEndAt = null;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("User unsuspended: userId={UserId}", userId);
            await _notificationService.notifySellerUnsuspended(userId);

            return new SuspendSellerResultDto { IsSuccess = true, Message = "Seller unsuspended." };
        }
        catch
        {
            return new SuspendSellerResultDto { IsSuccess = false, Message = "System error: Unable to unsuspend user." };
        }
    }
}
