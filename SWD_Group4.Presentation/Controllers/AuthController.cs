using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SWD_Group4.BusinessLogic.IServices;
using SWD_Group4.Presentation.Models;
using System.Security.Claims;

namespace SWD_Group4.Presentation.Controllers;

public sealed class AuthController : Controller
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterUserViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> registerUser(RegisterUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Register", model);
        }

        var result = await _authService.registerUser(model.Name, model.Email, model.Password);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View("Register", model);
        }

        TempData["Message"] = result.Message;
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Login()
    {
        ViewBag.Message = TempData["Message"] as string ?? string.Empty;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> loginUser(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Login", model);
        }

        var result = await _authService.loginUser(model.Email, model.Password);
        if (!result.IsSuccess || result.UserId is null)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View("Login", model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, result.UserId.Value.ToString()),
            new(ClaimTypes.Name, result.Name),
            new(ClaimTypes.Email, result.Email),
            new(ClaimTypes.Role, result.Role)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return RedirectToAction("ViewRefundRequests", "Refund");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }
}
