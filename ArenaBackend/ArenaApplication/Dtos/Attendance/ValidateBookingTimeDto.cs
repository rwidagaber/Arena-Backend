using System;

namespace ArenaApplication.Dtos.Attendance
{
    /// <summary>Request to check whether a proposed booking time is allowed (5h spacing).</summary>
    public class ValidateBookingTimeDto
    {
        public DateTime Date { get; set; }

        public TimeSpan StartTime { get; set; }
    }
}
