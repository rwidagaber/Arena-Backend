// QRCodeController.cs
using ArenaApplication.Dtos.QrCodeDtos;
using ArenaApplication.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/qr")]
//[Authorize]
public class QRCodeController : ControllerBase
{
    private readonly IQRCodeService _qrService;

    public QRCodeController(IQRCodeService qrService)
    {
        _qrService = qrService;
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
        var result = await _qrService.ScanAsync(dto.Code, dto.ScannedById);
        return Ok(result);
    }
}