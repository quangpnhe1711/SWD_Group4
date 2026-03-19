using SWD_Group4.BusinessLogic.DTO;
using SWD_Group4.DataAccess.Models;

namespace SWD_Group4.BusinessLogic.IServices;

public interface IVerificationService
{
    Task<SubmitVerificationRequestResultDto> submitVerificationRequest(int userId, SubmitVerificationRequestDto dto);

    Task<List<VerificationRequest>> getPendingRequests();

    Task<VerificationRequest?> getVerificationRequestDetail(int requestId);

    Task<ProcessVerificationRequestResultDto> processRequest(int requestId, bool isApproved, string? reason);

    Task<List<VerificationRequest>> getMyRequests(int userId);
}
