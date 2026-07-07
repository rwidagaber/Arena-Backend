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
        var result = await _qrService.GenerateAsync(bookingId);
        return Ok(result);
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

}
