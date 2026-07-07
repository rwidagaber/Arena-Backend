using ArenaApplication.IServices;
using ArenaInfrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ArenaMVC.Controllers;

[Authorize(Roles = "Admin")]
public class QRCheckInController : Controller
{
    private readonly IQRCodeService _qrService;
    private readonly IAnalyticsCacheVersionService _analyticsCacheVersionService;
    private readonly ILogger<QRCheckInController> _logger;
    private readonly AppDbContext _context;

    public QRCheckInController(
        IQRCodeService qrService,
        IAnalyticsCacheVersionService analyticsCacheVersionService,
        ILogger<QRCheckInController> logger,
        AppDbContext context)
    {
        _qrService = qrService;
        _analyticsCacheVersionService = analyticsCacheVersionService;
        _logger = logger;
        _context = context;
    }

    // GET: /QRCheckIn
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    // GET: /QRCheckIn/History
    [HttpGet]
    public async Task<IActionResult> History()
    {
        // High Performance: Use Select projection to fetch only scalar columns needed by the view.
        // Bypasses loading massive entities (Booking, User, MemberProfile) and avoids EF tracking.
        var attendances = await _context.Attendances
            .AsNoTracking()
            .OrderByDescending(a => a.CheckInTime)
            .Take(100)
            .Select(a => new AttendanceHistoryViewModel
            {
                BookingId = a.BookingId,
                MemberName = ((a.MemberProfile.User.FirstName ?? "") + " " + (a.MemberProfile.User.LastName ?? "")).Trim(),
                BookingDate = a.Booking.BookingDate,
                StartTime = a.Booking.StartTime,
                EndTime = a.Booking.EndTime,
                CheckInTime = a.CheckInTime,
                ScannedById = a.ScannedById
            })
            .ToListAsync();

        var adminIds = attendances.Where(a => a.ScannedById.HasValue).Select(a => a.ScannedById!.Value).Distinct().ToList();
        
        var adminMap = await _context.Users
            .AsNoTracking()
            .Where(u => adminIds.Contains(u.Id))
            .Select(u => new { u.Id, Name = (u.FirstName + " " + u.LastName).Trim() })
            .ToDictionaryAsync(u => u.Id, u => u.Name);

        ViewBag.AdminMap = adminMap;

        return View(attendances);
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

    // Lightweight DTO to transfer only data needed for rendering checkin list
    public class AttendanceHistoryViewModel
    {
        public Guid BookingId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public DateTime? CheckInTime { get; set; }
        public Guid? ScannedById { get; set; }
    }
}
