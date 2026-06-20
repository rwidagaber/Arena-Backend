using ArenaApplication.Dtos.Dashboard.Analytics;
using ArenaApplication.IServices;
using ArenaMVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArenaMVC.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("admin/analytics/v2")]
[Route("api/admin/analytics/v2")]
public class AdminAnalyticsController : Controller
{
  private readonly IDashboardService _dashboardService;
  private readonly IDashboardDataSeeder _seeder;
  private readonly IAnalyticsCacheVersionService _cacheVersion;

  public AdminAnalyticsController(
      IDashboardService dashboardService,
      IDashboardDataSeeder seeder,
      IAnalyticsCacheVersionService cacheVersion)
  {
    _dashboardService = dashboardService;
    _seeder = seeder;
    _cacheVersion = cacheVersion;
  }

  [HttpGet("/admin/dashboard")]
  [HttpGet("/admin/analytics")]
  public IActionResult Dashboard()
  {
    return View();
  }

  [HttpPost("/admin/analytics/generate-demo-data")]
  public async Task<IActionResult> GenerateDemoData()
  {
    await _seeder.SeedAsync(forceReseed: true);
    _cacheVersion.BumpVersion();   // invalidate all cached analytics immediately
    TempData["DemoDataSuccess"] = "Dashboard demo data generated successfully.";
    return Redirect("/admin/dashboard");
  }

  [HttpGet("overview")]
  public async Task<IActionResult> GetOverview(
      [FromQuery] DateTime? startDateUtc,
      [FromQuery] DateTime? endDateUtc,
      [FromQuery] string timezone = "UTC",
      CancellationToken cancellationToken = default)
  {
    var query = new AnalyticsQueryWindowDto
    {
      StartDateUtc = startDateUtc,
      EndDateUtc = endDateUtc,
      Timezone = timezone
    };

    var response = await _dashboardService.GetAnalyticsV2Async(query, cancellationToken);
    return Ok(response);
  }

  [HttpGet("drilldowns/revenue-daily")]
  public async Task<IActionResult> GetRevenueDaily(
      [FromQuery] DateTime? startDateUtc,
      [FromQuery] DateTime? endDateUtc,
      [FromQuery] string timezone = "UTC",
      CancellationToken cancellationToken = default)
  {
    var query = new AnalyticsQueryWindowDto
    {
      StartDateUtc = startDateUtc,
      EndDateUtc = endDateUtc,
      Timezone = timezone
    };

    var response = await _dashboardService.GetRevenueDrilldownAsync(query, cancellationToken);
    return Ok(response);
  }

  [HttpGet("drilldowns/attendance-daily")]
  public async Task<IActionResult> GetAttendanceDaily(
      [FromQuery] DateTime? startDateUtc,
      [FromQuery] DateTime? endDateUtc,
      [FromQuery] string timezone = "UTC",
      CancellationToken cancellationToken = default)
  {
    var query = new AnalyticsQueryWindowDto
    {
      StartDateUtc = startDateUtc,
      EndDateUtc = endDateUtc,
      Timezone = timezone
    };

    var response = await _dashboardService.GetAttendanceDrilldownAsync(query, cancellationToken);
    return Ok(response);
  }
}