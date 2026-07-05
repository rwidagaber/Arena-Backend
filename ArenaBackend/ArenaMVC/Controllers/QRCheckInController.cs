using ArenaApplication.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ArenaMVC.Controllers;

[Authorize(Roles = "Admin")]
public class QRCheckInController : Controller
{
    private readonly IQRCodeService _qrService;
    private readonly IAnalyticsCacheVersionService _analyticsCacheVersionService;
    private readonly ILogger<QRCheckInController> _logger;

    public QRCheckInController(
        IQRCodeService qrService,
        IAnalyticsCacheVersionService analyticsCacheVersionService,
        ILogger<QRCheckInController> logger)
    {
        _qrService = qrService;
        _analyticsCacheVersionService = analyticsCacheVersionService;
        _logger = logger;
    }

    // GET: /QRCheckIn
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    // POST: /QRCheckIn/Scan  (AJAX — returns JSON)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Scan([FromBody] ScanRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Code))
        {
            return Json(new
            {
                success = false,
                message = "QR code value is empty."
            });
        }

        try
        {
            var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _qrService.ScanAsync(request.Code, adminId);

            _analyticsCacheVersionService.BumpVersion();

            var isSuccess = result.BookingId.HasValue
                            && !result.IsExpired
                            && !result.IsAlreadyUsed;

            return Json(new
            {
                success = isSuccess,
                message = result.Message,
                memberName = result.MemberName,
                sessionDate = result.SessionDate?.ToString("yyyy-MM-dd"),
                startTime = result.StartTime?.ToString(@"hh\:mm"),
                endTime = result.EndTime?.ToString(@"hh\:mm"),
                bookingId = result.BookingId,
                isExpired = result.IsExpired,
                isAlreadyUsed = result.IsAlreadyUsed
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning QR code: {Code}", request.Code);
            return Json(new
            {
                success = false,
                message = "An unexpected error occurred while processing the QR code."
            });
        }
    }

    public class ScanRequest
    {
        public string Code { get; set; } = string.Empty;
    }
}
