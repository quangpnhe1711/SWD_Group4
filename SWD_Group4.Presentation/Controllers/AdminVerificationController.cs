using Microsoft.AspNetCore.Mvc;
using SWD_Group4.BusinessLogic.IServices;
using SWD_Group4.Presentation.Models;

namespace SWD_Group4.Presentation.Controllers;

// Demo mode: no strict role-gating on this controller
public sealed class AdminVerificationController : Controller
{
    private readonly IVerificationService _verificationService;
    private readonly IUserAdminService _userAdminService;
    private readonly ISuspensionService _suspensionService;

    public AdminVerificationController(IVerificationService verificationService, IUserAdminService userAdminService, ISuspensionService suspensionService)
    {
        _verificationService = verificationService;
        _userAdminService = userAdminService;
        _suspensionService = suspensionService;
    }

    [HttpGet]
    public async Task<IActionResult> viewPendingRequests()
    {
        ViewBag.Message = TempData["Message"] as string ?? string.Empty;
        var pending = await _verificationService.getPendingRequests();
        return View(pending);
    }

    // Flow step: from pending list -> get detail
    [HttpGet]
    public async Task<IActionResult> getDetail(int requestId)
    {
        var request = await _verificationService.getVerificationRequestDetail(requestId);
        if (request == null)
        {
            TempData["Message"] = "Request not found.";
            return RedirectToAction(nameof(viewPendingRequests));
        }

        ViewBag.RejectModel = new RejectVerificationRequestViewModel { RequestId = requestId };
        return View("requestDetails", request);
    }

    // Backwards-compatible action name (used by existing views/links)
    [HttpGet]
    public Task<IActionResult> requestDetails(int requestId) => getDetail(requestId);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> approveRequest(int requestId)
    {
        var result = await _verificationService.processRequest(requestId, true, null);
        TempData["Message"] = result.Message;

        if (!result.IsSuccess)
        {
            return RedirectToAction(nameof(getDetail), new { requestId });
        }

        return RedirectToAction(nameof(viewPendingRequests));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> rejectRequest(RejectVerificationRequestViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Message"] = "Invalid rejection reason.";
            return RedirectToAction(nameof(getDetail), new { requestId = model.RequestId });
        }

        var result = await _verificationService.processRequest(model.RequestId, false, model.Reason);
        TempData["Message"] = result.Message;

        if (!result.IsSuccess)
        {
            return RedirectToAction(nameof(getDetail), new { requestId = model.RequestId });
        }

        return RedirectToAction(nameof(viewPendingRequests));
    }

    [HttpGet]
    public async Task<IActionResult> viewSellerList()
    {
        ViewBag.Message = TempData["Message"] as string ?? string.Empty;
        var sellers = await _userAdminService.getSellerList();
        return View(sellers);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> suspendSeller(SuspendSellerViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            TempData["Message"] = errors.Count > 0
                ? ("Invalid suspend request: " + string.Join(" | ", errors))
                : "Invalid suspend request.";

            return RedirectToAction(nameof(viewSellerList));
        }

        var result = await _suspensionService.suspendUser(model.UserId, model.Reason, model.DurationType);
        TempData["Message"] = result.Message;
        return RedirectToAction(nameof(viewSellerList));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> unsuspendSeller(int userId)
    {
        var result = await _suspensionService.unsuspendUser(userId);
        TempData["Message"] = result.Message;
        return RedirectToAction(nameof(viewSellerList));
    }
}
