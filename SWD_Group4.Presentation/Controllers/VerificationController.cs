using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using SWD_Group4.BusinessLogic.DTO;
using SWD_Group4.BusinessLogic.IServices;
using SWD_Group4.Presentation.Models;
using System.IO;
using System.Security.Claims;

namespace SWD_Group4.Presentation.Controllers;

[Authorize]
public sealed class VerificationController : Controller
{
    private const long MaxUploadBytes = 5 * 1024 * 1024; // 5 MB
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private readonly IVerificationService _verificationService;
    private readonly IWebHostEnvironment _environment;

    public VerificationController(IVerificationService verificationService, IWebHostEnvironment environment)
    {
        _verificationService = verificationService;
        _environment = environment;
    }

    [HttpGet]
    public IActionResult Submit()
    {
        ViewBag.Message = TempData["Message"] as string ?? string.Empty;
        return View(new SubmitVerificationRequestViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> submitVerificationRequest(SubmitVerificationRequestViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Submit", model);
        }

        var userId = GetUserId();
        if (userId == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        var citizenFrontUrl = await SaveUploadOrUseUrl(model.CitizenImageFile, model.CitizenImage, "citizen_front");
        var citizenBackUrl = await SaveUploadOrUseUrl(model.CitizenImageBackFile, model.CitizenImageBack, "citizen_back");
        var bankCardUrl = await SaveUploadOrUseUrl(model.BankCardImageFile, model.BankCardImage, "bank_card");

        if (citizenFrontUrl == null || citizenBackUrl == null || bankCardUrl == null)
        {
            // SaveUploadOrUseUrl has added ModelState errors already.
            return View("Submit", model);
        }

        var dto = new SubmitVerificationRequestDto
        {
            Url = model.Url,
            CitizenId = model.CitizenId,
            BankAccount = model.BankAccount,
            BankName = model.BankName,
            CitizenImage = citizenFrontUrl,
            CitizenImageBack = citizenBackUrl,
            BankCardImage = bankCardUrl
        };

        var result = await _verificationService.submitVerificationRequest(userId.Value, dto);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View("Submit", model);
        }

        TempData["Message"] = result.Message;
        return RedirectToAction(nameof(MyRequests));
    }

    private async Task<string?> SaveUploadOrUseUrl(IFormFile? file, string? manualUrl, string nameForError)
    {
        if (file != null && file.Length > 0)
        {
            if (file.Length > MaxUploadBytes)
            {
                ModelState.AddModelError(string.Empty, $"{nameForError}: file is too large (max 5MB).");
                return null;
            }

            var ext = Path.GetExtension(file.FileName) ?? string.Empty;
            if (!AllowedExtensions.Contains(ext))
            {
                ModelState.AddModelError(string.Empty, $"{nameForError}: only .jpg/.jpeg/.png/.webp are allowed.");
                return null;
            }

            var uploadsRelative = Path.Combine("uploads", "verification");
            var uploadsPhysical = Path.Combine(_environment.WebRootPath, uploadsRelative);
            Directory.CreateDirectory(uploadsPhysical);

            var fileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
            var physicalPath = Path.Combine(uploadsPhysical, fileName);

            await using (var stream = System.IO.File.Create(physicalPath))
            {
                await file.CopyToAsync(stream);
            }

            // Return web URL (static files).
            return "/" + uploadsRelative.Replace("\\", "/") + "/" + fileName;
        }

        if (!string.IsNullOrWhiteSpace(manualUrl))
        {
            return manualUrl.Trim();
        }

        ModelState.AddModelError(string.Empty, $"{nameForError}: image is required.");
        return null;
    }

    [HttpGet]
    public async Task<IActionResult> MyRequests()
    {
        ViewBag.Message = TempData["Message"] as string ?? string.Empty;

        var userId = GetUserId();
        if (userId == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        var requests = await _verificationService.getMyRequests(userId.Value);
        return View(requests);
    }

    private int? GetUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, out var id) ? id : null;
    }
}
