using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Dtos.AttendanceDtos
{
    public class CreateAttendanceDto
    {
        public Guid BookingId { get; set; }

        public Guid MemberProfileId { get; set; }

        public Guid? ScannedById { get; set; }
    }
}
