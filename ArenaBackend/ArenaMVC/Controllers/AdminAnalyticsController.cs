using ArenaApplication.Dtos.Dashboard.Analytics;
using ArenaApplication.IServices;
using Microsoft.AspNetCore.Mvc;

namespace ArenaMVC.Controllers;

[ApiController]
[Route("admin/analytics/v2")]
[Route("api/admin/analytics/v2")]
public class AdminAnalyticsController : Controller
{
  private readonly IDashboardService _dashboardService;

  public AdminAnalyticsController(IDashboardService dashboardService)
  {
    _dashboardService = dashboardService;
  }

  [HttpGet("/admin/dashboard")]
  [HttpGet("/admin/analytics")]
  public IActionResult Dashboard()
  {
    return View();
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