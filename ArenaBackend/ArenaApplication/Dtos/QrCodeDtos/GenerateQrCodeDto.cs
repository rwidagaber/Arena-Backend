using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Dtos.QrCodeDtos
{
    public class GenerateQrCodeDto
    {
        public Guid BookingId { get; set; }

        public DateTime ExpirationTime { get; set; }
    }
}
