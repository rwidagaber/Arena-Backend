using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ArenaMVC.Models;
using ArenaApplication.Services.SubscriptionPlan;

namespace ArenaMVC.Controllers;

public class HomeController : Controller
{
    private readonly ISubscriptionPlanService _subscriptionPlanService;

    public HomeController(ISubscriptionPlanService subscriptionPlanService)
    {
        _subscriptionPlanService = subscriptionPlanService;
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
