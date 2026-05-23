using ArenaDomain.Shared;

namespace ArenaDomain.Entities.Bookings
{
    public class Attendance : BaseEntity<Guid>
    {
        public Guid BookingId { get; set; }

        public virtual Booking Booking { get; set; } = null!;

        public Guid MemberProfileId { get; set; }

        public virtual MemberProfile MemberProfile { get; set; } = null!;

        public DateTime? CheckInTime { get; set; }

        public Guid? ScannedById { get; set; }
    }
}
