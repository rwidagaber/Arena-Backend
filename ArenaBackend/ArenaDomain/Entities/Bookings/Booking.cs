using ArenaDomain.Enums;
using ArenaDomain.Shared;

namespace ArenaDomain.Entities.Bookings
{
    public class Booking : BaseEntity<Guid>
    {
        public Guid MemberProfileId { get; set; }

        public virtual MemberProfile MemberProfile { get; set; } = null!;

        public DateTime BookingDate { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan? EndTime { get; set; }

        public BookingStatus Status { get; set; }

        public BookingSource Source { get; set; }

        // Navigation
        public virtual QRCode? QRCode { get; set; }

        public virtual Attendance? Attendance { get; set; }
    }
}
