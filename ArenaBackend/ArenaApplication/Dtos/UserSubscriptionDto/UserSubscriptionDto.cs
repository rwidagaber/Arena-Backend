using ArenaDomain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Dtos.UserSupscriptionDto
{
    public class UserSubscriptionDto
    {
        public Guid Id { get; set; }
        public string PlanNameEn { get; set; } = null!;
        public string PlanNameAr { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public SubscriptionStatus Status { get; set; }
        public int RemainingSessions { get; set; }
        public bool ReminderSent { get; set; }
    }
}
