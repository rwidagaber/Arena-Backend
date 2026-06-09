namespace ArenaApplication.Dtos.QrCodeDtos
{
    public class ScanQrRequestDto
    {
        public string Code { get; set; } = string.Empty;
        public Guid ScannedById { get; set; }
    }
}