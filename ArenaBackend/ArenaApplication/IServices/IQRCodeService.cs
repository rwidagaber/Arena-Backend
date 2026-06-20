using ArenaApplication.Dtos.QrCodeDtos;

namespace ArenaApplication.IServices
{
    public interface IQRCodeService
    {
        Task<QrDto> GenerateAsync(Guid bookingId);
        Task<QrScanResultDto> ScanAsync(string code, Guid? scannedById);
    }
}
