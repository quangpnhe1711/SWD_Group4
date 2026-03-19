using Microsoft.EntityFrameworkCore;
using SWD_Group4.BusinessLogic.DTO;
using SWD_Group4.BusinessLogic.IServices;
using SWD_Group4.DataAccess.Context;
using SWD_Group4.DataAccess.Models;

namespace SWD_Group4.BusinessLogic.Services;

public sealed class VerificationService : IVerificationService
{
    private const string StatusProcess = "PROCESS";
    private const string StatusApproved = "APPROVED";
    private const string StatusRejected = "REJECTED";

    private readonly BookStoreContext _dbContext;
    private readonly INotificationService _notificationService;

    public VerificationService(BookStoreContext dbContext, INotificationService notificationService)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
    }

    public async Task<SubmitVerificationRequestResultDto> submitVerificationRequest(int userId, SubmitVerificationRequestDto dto)
    {
        try
        {
            var validationError = await validateSubmit(userId, dto);
            if (!string.IsNullOrWhiteSpace(validationError))
            {
                return new SubmitVerificationRequestResultDto { IsSuccess = false, Message = validationError };
            }

            var request = new VerificationRequest
            {
                UserId = userId,
                Status = StatusProcess,
                Url = dto.Url?.Trim(),
                CitizenId = dto.CitizenId.Trim(),
                BankAccount = dto.BankAccount.Trim(),
                BankName = dto.BankName.Trim(),
                CitizenImage = dto.CitizenImage.Trim(),
                CitizenImageBack = dto.CitizenImageBack.Trim(),
                BankCardImage = dto.BankCardImage.Trim(),
                CreatedAt = DateTime.Now
            };

            _dbContext.VerificationRequests.Add(request);
            await _dbContext.SaveChangesAsync();

            return new SubmitVerificationRequestResultDto
            {
                IsSuccess = true,
                RequestId = request.Id,
                Message = "Verification request submitted."
            };
        }
        catch
        {
            return new SubmitVerificationRequestResultDto
            {
                IsSuccess = false,
                Message = "System error: Unable to submit verification request. Please try again later."
            };
        }
    }

    public async Task<List<VerificationRequest>> getPendingRequests()
    {
        return await _dbContext.VerificationRequests
            .AsNoTracking()
            .Include(r => r.User)
            .Where(r => r.Status == StatusProcess)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<VerificationRequest?> getVerificationRequestDetail(int requestId)
    {
        return await _dbContext.VerificationRequests
            .AsNoTracking()
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == requestId);
    }

    public async Task<ProcessVerificationRequestResultDto> processRequest(int requestId, bool isApproved, string? reason)
    {
        try
        {
            var request = await _dbContext.VerificationRequests
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null)
            {
                return new ProcessVerificationRequestResultDto { IsSuccess = false, Message = "Request not found." };
            }

            // BR-KYC-03: Request Finality
            if (!string.Equals(request.Status, StatusProcess, StringComparison.OrdinalIgnoreCase))
            {
                return new ProcessVerificationRequestResultDto
                {
                    IsSuccess = false,
                    Message = "This request has already been processed and cannot be modified."
                };
            }

            if (!isApproved)
            {
                // BR-OPS-04: reason required (min 20 chars)
                if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 20)
                {
                    return new ProcessVerificationRequestResultDto
                    {
                        IsSuccess = false,
                        Message = "Rejection reason is required (minimum 20 characters)."
                    };
                }

                request.Status = StatusRejected;
                request.Approved = false;
                request.Reason = reason.Trim();
                request.UpdatedAt = DateTime.Now;

                await _dbContext.SaveChangesAsync();
                await _notificationService.sendVerifyNotification(request.UserId, false, request.Reason);

                return new ProcessVerificationRequestResultDto { IsSuccess = true, Message = "Request rejected." };
            }

            // Approval path: detect identity conflicts (UC A2)
            var conflictMessage = await detectIdentityConflict(request.UserId, request.CitizenId, request.BankAccount);
            if (!string.IsNullOrWhiteSpace(conflictMessage))
            {
                return new ProcessVerificationRequestResultDto { IsSuccess = false, Message = conflictMessage };
            }

            // BR-ACC-04: identifiers cannot be changed once verified by admin
            if (!string.IsNullOrWhiteSpace(request.User.CitizenId)
                && !string.Equals(request.User.CitizenId, request.CitizenId, StringComparison.OrdinalIgnoreCase))
            {
                return new ProcessVerificationRequestResultDto
                {
                    IsSuccess = false,
                    Message = "Conflict: This account already has a verified Citizen ID and cannot change it."
                };
            }

            if (!string.IsNullOrWhiteSpace(request.User.BankAccount)
                && !string.Equals(request.User.BankAccount, request.BankAccount, StringComparison.OrdinalIgnoreCase))
            {
                return new ProcessVerificationRequestResultDto
                {
                    IsSuccess = false,
                    Message = "Conflict: This account already has a verified bank account and cannot change it."
                };
            }

            using var tx = await _dbContext.Database.BeginTransactionAsync();

            request.Status = StatusApproved;
            request.Approved = true;
            request.Reason = null;
            request.UpdatedAt = DateTime.Now;

            request.User.Url = request.Url;
            request.User.CitizenId = request.CitizenId;
            request.User.BankAccount = request.BankAccount;
            request.User.BankName = request.BankName;
            request.User.CitizenImage = request.CitizenImage;
            request.User.CitizenImageBack = request.CitizenImageBack;

            await _dbContext.SaveChangesAsync();
            await tx.CommitAsync();

            await _notificationService.sendVerifyNotification(request.UserId, true, null);

            return new ProcessVerificationRequestResultDto { IsSuccess = true, Message = "Request approved. Verified information has been updated." };
        }
        catch
        {
            return new ProcessVerificationRequestResultDto
            {
                IsSuccess = false,
                Message = "System error: Unable to process the verification request. Please try again later."
            };
        }
    }

    public async Task<List<VerificationRequest>> getMyRequests(int userId)
    {
        return await _dbContext.VerificationRequests
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    private async Task<string?> validateSubmit(int userId, SubmitVerificationRequestDto dto)
    {
        if (userId <= 0)
        {
            return "Invalid user.";
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return "User not found.";
        }

        if (!string.Equals(user.Status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            return "Account is inactive.";
        }

        // If core KYC identifiers are already verified on the account, do not allow re-submission.
        // BR-ACC-04: identifiers cannot be edited through standard updates once verified by Admin.
        if (!string.IsNullOrWhiteSpace(user.CitizenId) || !string.IsNullOrWhiteSpace(user.BankAccount))
        {
            return "Your account already has verified information.";
        }

        if (await _dbContext.VerificationRequests.AnyAsync(r => r.UserId == userId && r.Status == StatusProcess))
        {
            return "You already have a pending verification request.";
        }

        if (string.IsNullOrWhiteSpace(dto.CitizenId)
            || string.IsNullOrWhiteSpace(dto.BankAccount)
            || string.IsNullOrWhiteSpace(dto.BankName)
            || string.IsNullOrWhiteSpace(dto.CitizenImage)
            || string.IsNullOrWhiteSpace(dto.CitizenImageBack)
            || string.IsNullOrWhiteSpace(dto.BankCardImage))
        {
            return "Missing required fields";
        }

        var conflictMessage = await detectIdentityConflict(userId, dto.CitizenId, dto.BankAccount);
        if (!string.IsNullOrWhiteSpace(conflictMessage))
        {
            return conflictMessage;
        }

        return null;
    }

    private async Task<string?> detectIdentityConflict(int userId, string? citizenId, string? bankAccount)
    {
        var trimmedCitizenId = citizenId?.Trim();
        var trimmedBankAccount = bankAccount?.Trim();

        if (!string.IsNullOrWhiteSpace(trimmedCitizenId))
        {
            var citizenUsedByOtherUser = await _dbContext.Users.AnyAsync(u => u.Id != userId && u.CitizenId != null && u.CitizenId == trimmedCitizenId);
            var citizenUsedByOtherPending = await _dbContext.VerificationRequests.AnyAsync(r => r.UserId != userId && r.Status == StatusProcess && r.CitizenId != null && r.CitizenId == trimmedCitizenId);

            if (citizenUsedByOtherUser || citizenUsedByOtherPending)
            {
                return "Conflict: This identity information is already registered to another account.";
            }
        }

        if (!string.IsNullOrWhiteSpace(trimmedBankAccount))
        {
            var bankUsedByOtherUser = await _dbContext.Users.AnyAsync(u => u.Id != userId && u.BankAccount != null && u.BankAccount == trimmedBankAccount);
            var bankUsedByOtherPending = await _dbContext.VerificationRequests.AnyAsync(r => r.UserId != userId && r.Status == StatusProcess && r.BankAccount != null && r.BankAccount == trimmedBankAccount);

            if (bankUsedByOtherUser || bankUsedByOtherPending)
            {
                return "Conflict: This identity information is already registered to another account.";
            }
        }

        return null;
    }
}
