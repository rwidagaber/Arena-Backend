// QRCodeController.cs
using ArenaApplication.Dtos.QrCodeDtos;
using ArenaApplication.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("api/qr")]
[Authorize]
public class QRCodeController : ControllerBase
{
    private readonly IQRCodeService _qrService;
    private readonly IAnalyticsCacheVersionService _analyticsCacheVersionService;

    public QRCodeController(IQRCodeService qrService, IAnalyticsCacheVersionService analyticsCacheVersionService)
    {
        _qrService = qrService;
        _analyticsCacheVersionService = analyticsCacheVersionService;
    }

    // Member calls this after booking is confirmed
    [HttpPost("generate/{bookingId}")]
    public async Task<IActionResult> Generate(Guid bookingId)
    {
        try
        {
            var result = await _qrService.GenerateAsync(bookingId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // Admin calls this when member arrives
    [HttpPost("scan")]
    public async Task<IActionResult> Scan([FromBody] ScanQrRequestDto dto)
    {
        var scannedById = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _qrService.ScanAsync(dto.Code, scannedById);
        _analyticsCacheVersionService.BumpVersion();
        return Ok(result);
    }

    // Phone camera scanners open QR links with GET, so this endpoint performs the check-in directly.
    [AllowAnonymous]
    [HttpGet("scan/{code}")]
    public async Task<IActionResult> ScanFromCamera(string code)
    {
        var result = await _qrService.ScanAsync(code, null);
        _analyticsCacheVersionService.BumpVersion();

        var title = result.IsExpired
            ? "QR expired"
            : result.IsAlreadyUsed
                ? "QR already scanned"
                : result.BookingId.HasValue
                    ? "Attendance saved"
                    : "Invalid QR code";

        var html = $$"""
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>{{title}}</title>
              <style>
                :root { color-scheme: light dark; font-family: Inter, Arial, sans-serif; }
                body { margin: 0; min-height: 100vh; display: grid; place-items: center; background: #f8fafc; color: #0f172a; }
                main { width: min(92vw, 440px); padding: 28px; border: 1px solid #e2e8f0; border-radius: 12px; background: #fff; box-shadow: 0 18px 45px rgba(15,23,42,.12); }
                .badge { display: inline-flex; padding: 8px 12px; border-radius: 999px; background: #ecfccb; color: #3f6212; font-weight: 800; font-size: 12px; text-transform: uppercase; }
                h1 { margin: 18px 0 8px; font-size: 30px; line-height: 1.1; }
                p { margin: 0; color: #64748b; line-height: 1.6; }
                code { display: block; margin-top: 18px; padding: 12px; border-radius: 8px; background: #f1f5f9; overflow-wrap: anywhere; font-size: 12px; }
                @media (prefers-color-scheme: dark) {
                  body { background: #020617; color: #e5e7eb; }
                  main { background: #0f172a; border-color: #263449; }
                  p { color: #94a3b8; }
                  code { background: #111c2f; }
                }
              </style>
            </head>
            <body>
              <main>
                <span class="badge">Arena check-in</span>
                <h1>{{title}}</h1>
                <p>{{result.Message}}</p>
                {{(result.BookingId.HasValue ? $"<code>Booking: {result.BookingId}</code>" : string.Empty)}}
              </main>
            </body>
            </html>
            """;

        return Content(html, "text/html");
    }
}
