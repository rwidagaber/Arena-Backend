using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Dtos.QrCodeDtos
{
    internal class QrDto
    {
        public Guid Id { get; set; }

        public string Code { get; set; } = string.Empty;

        public DateTime GeneratedAt { get; set; }

        public DateTime ExpirationTime { get; set; }

        public bool IsUsed { get; set; }

        public Guid BookingId { get; set; }

    }
}
