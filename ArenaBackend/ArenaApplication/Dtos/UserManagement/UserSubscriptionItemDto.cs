using System;

namespace ArenaApplication.Dtos.UserManagement
{
    public class UserSubscriptionItemDto
    {
        public string PlanName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int RemainingSessions { get; set; }
        public int DurationDays { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsManualActive { get; set; }
    }
}
