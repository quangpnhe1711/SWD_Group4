using SWD_Group4.BusinessLogic.DTO;
using SWD_Group4.DataAccess.Models;

namespace SWD_Group4.BusinessLogic.IServices;

public interface IRefundService
{
    Task<List<RefundDto>> GetRefundRequestsAsync(int sellerId);

    Task<bool> ApproveRefundAsync(int refundId);

    Task<bool> RejectRefundAsync(int refundId, string reason);

    Task<bool> ProcessRefundAsync(Order order, RefundRequest refundRequest);

    Task<bool> UpdateStatusAsync(RefundRequest refundRequest, string refundRequestStatus);
}
