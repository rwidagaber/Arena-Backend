using ArenaDomain.Entities.Payments;
using ArenaDomain.Enums;
using ArenaDomain.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaDomain.Entities.Subscription
{
    public class UserSubscription : BaseEntity<Guid>
    {
        public Guid MemberProfileId { get; set; }

        public virtual MemberProfile MemberProfile { get; set; } = null!;

        public Guid PlanId { get; set; }

        public virtual SubscriptionPlan Plan { get; set; } = null!;

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public SubscriptionStatus Status { get; set; }

        public int RemainingSessions { get; set; }

        public bool ReminderSent { get; set; }

        // Navigation
        public virtual ICollection<Payment> Payments { get; set; } = [];
    }
}
