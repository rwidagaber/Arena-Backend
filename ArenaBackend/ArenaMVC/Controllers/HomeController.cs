using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ArenaMVC.Models;
using ArenaApplication.Services.SubscriptionPlan;
using ArenaDomain.Shared;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Localization;

namespace ArenaMVC.Controllers;

public class HomeController : Controller
{
    private readonly ISubscriptionPlanService _subscriptionPlanService;
    private readonly IStringLocalizer<ArenaLocalization> _localizer;

    public HomeController(ISubscriptionPlanService subscriptionPlanService, IStringLocalizer<ArenaLocalization> localizer)
    {
        _subscriptionPlanService = subscriptionPlanService;
        _localizer = localizer;
    }

    [HttpPost]
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
            var plans = await _subscriptionPlanService.GetAllAsync(cancellationToken);
            var plansList = plans.ToList();
            ViewBag.TotalPlans = plansList.Count;
            ViewBag.ActivePlans = plansList.Count(p => p.IsActive);
        }
        catch
        {
            ViewBag.TotalPlans = 0;
            ViewBag.ActivePlans = 0;
        }

        return View();
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
