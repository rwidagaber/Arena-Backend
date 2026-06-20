using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Dtos.AttendanceDtos
{
    public class AttendanceResponseDto
    {
        public Guid Id { get; set; }

        public Guid BookingId { get; set; }

        public Guid MemberProfileId { get; set; }

        public DateTime? CheckInTime { get; set; }

        public Guid? ScannedById { get; set; }

       
    }
}
