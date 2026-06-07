using System;

namespace ArenaMVC.Models
{
    public class SubscriptionItemViewModel
    {
        public string PlanName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int RemainingSessions { get; set; }
        public int DurationDays { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
