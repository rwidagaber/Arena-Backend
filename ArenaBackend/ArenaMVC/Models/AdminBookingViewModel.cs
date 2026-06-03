using ArenaDomain.Enums;
using System;

namespace ArenaMVC.Models
{
    public class AdminBookingViewModel
    {
        public Guid Id { get; set; }
        public string MemberProfileName { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public BookingStatus Status { get; set; }
    }
}
