using System.Security.Claims;
using ArenaMVC.Models;
using ArenaMVC.Services;
using ArenaDomain.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace ArenaMVC.Controllers;

public class AuthController : Controller
{
    private readonly IMvcAdminLoginService _loginService;
    private readonly IStringLocalizer<ArenaLocalization> _localizer;

    public AuthController(
        IMvcAdminLoginService loginService,
        IStringLocalizer<ArenaLocalization> localizer)
    {
        _loginService = loginService;
        _localizer = localizer;
    }

    // ── GET /Auth/Login ───────────────────────────────────────────────────
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        // Redirect already-authenticated admins to home
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToLocal(returnUrl);

        var model = new AdminLoginViewModel { ReturnUrl = returnUrl };
        return View(model);
    }

    // ── POST /Auth/Login ──────────────────────────────────────────────────
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(AdminLoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _loginService.ValidateAdminAsync(model.Email, model.Password);

        if (result is null)
        {
            ModelState.AddModelError(string.Empty, _localizer["InvalidEmailOrPassword"]);
            return View(model);
        }

        // Build the cookie claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, result.UserId),
            new Claim(ClaimTypes.Email,           result.Email),
            new Claim(ClaimTypes.Name,            result.FullName),
            new Claim(ClaimTypes.Role,            result.Role)
        };

        var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var authProps = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc   = model.RememberMe
                ? DateTimeOffset.UtcNow.AddDays(30)
                : DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            authProps);

        return RedirectToLocal(model.ReturnUrl);
    }

    // ── GET /Auth/Logout ──────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    // ── GET /Auth/AccessDenied ────────────────────────────────────────────
    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    // ─────────────────────────────────────────────────────────────────────
    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }
}
