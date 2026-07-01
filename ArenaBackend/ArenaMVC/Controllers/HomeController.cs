using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ArenaMVC.Models;
using ArenaApplication.IServices;
using ArenaDomain.Shared;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Authorization;

namespace ArenaMVC.Controllers;

[Authorize(Roles = "Admin")]
public class HomeController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly IStringLocalizer<ArenaLocalization> _localizer;

    public HomeController(IDashboardService dashboardService, IStringLocalizer<ArenaLocalization> localizer)
    {
        _dashboardService = dashboardService;
        _localizer = localizer;
    }

    [HttpPost]
    [AllowAnonymous]
    public IActionResult SetCulture(string culture, string returnUrl)
    {
        var response = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture));
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            response,
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true }
        );
        return LocalRedirect(returnUrl ?? "/");
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            var dashboard = await _dashboardService.GetDashboardDataAsync(cancellationToken);
            return View(dashboard);
        }
        catch (Exception ex)
        {
            try
            {
                System.IO.File.WriteAllText("dashboard_error.log", ex.ToString());
            }
            catch {}
            // Return an empty dashboard DTO so the view doesn't break
            return View(new ArenaApplication.Dtos.Dashboard.AdminDashboardDto());
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
