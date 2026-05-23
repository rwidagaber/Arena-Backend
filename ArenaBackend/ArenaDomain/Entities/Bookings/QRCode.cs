using ArenaDomain.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaDomain.Entities.Bookings
{
    public class QRCode : BaseEntity<Guid>
    {
        public Guid BookingId { get; set; }

        public virtual Booking Booking { get; set; } = null!;

        public string Code { get; set; } = string.Empty;

        public DateTime GeneratedAt { get; set; }

        public DateTime ExpirationTime { get; set; }

        public bool IsUsed { get; set; }
    }
}
