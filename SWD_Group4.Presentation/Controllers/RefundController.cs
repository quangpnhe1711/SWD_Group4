using Microsoft.AspNetCore.Mvc;
using SWD_Group4.BusinessLogic.IServices;
using SWD_Group4.Presentation.Models;

namespace SWD_Group4.Presentation.Controllers;

public sealed class RefundController : Controller
{
    private readonly IRefundService _refundService;

    public RefundController(IRefundService refundService)
    {
        _refundService = refundService;
    }

    [HttpGet]
    public async Task<IActionResult> ViewRefundRequests(int sellerId = 1)
    {
        var refunds = await _refundService.GetRefundRequestsAsync(sellerId);

        var model = new RefundListPageViewModel
        {
            SellerId = sellerId,
            Refunds = refunds,
            Message = refunds.Count == 0
                ? "No refund request available"
                : TempData["Message"] as string ?? string.Empty
        };

        return View("Index", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveRefund(int sellerId, int refundId, string decision = "confirm")
    {
        if (decision.Equals("cancel", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Message"] = "Approval was cancelled.";
            return RedirectToAction(nameof(ViewRefundRequests), new { sellerId });
        }

        var ok = await _refundService.ApproveRefundAsync(refundId);
        TempData["Message"] = ok
            ? "Refund approved and processed successfully."
            : "Approve refund failed. Please check request status or payment transaction.";

        return RedirectToAction(nameof(ViewRefundRequests), new { sellerId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectRefund(int sellerId, int refundId, string reason, string decision = "confirm")
    {
        if (decision.Equals("cancel", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Message"] = "Rejection was cancelled.";
            return RedirectToAction(nameof(ViewRefundRequests), new { sellerId });
        }

        var ok = await _refundService.RejectRefundAsync(refundId, reason);
        TempData["Message"] = ok
            ? "Refund rejected successfully."
            : "Reject refund failed. Reason is required and request must be pending.";

        return RedirectToAction(nameof(ViewRefundRequests), new { sellerId });
    }
}
