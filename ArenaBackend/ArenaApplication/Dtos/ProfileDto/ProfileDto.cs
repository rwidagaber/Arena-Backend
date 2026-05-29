using ArenaApplication.Dtos.SubscriptionPlan;
using ArenaDomain.Entities.Subscription;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Dtos.ProfileDto
{
    public class subscriptionPlanDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Email { get; set; } = null!;

        public bool IsActive { get; set; }

        public double? Weight { get; set; }
        public double? Height { get; set; }
        public double? BMI { get; set; }
        public string? Gender { get; set; }
        public string? ProfileImage { get; set; }



        public SubscriptionPlanDto? SubscriptionPlan { get; set; }
    }
}
