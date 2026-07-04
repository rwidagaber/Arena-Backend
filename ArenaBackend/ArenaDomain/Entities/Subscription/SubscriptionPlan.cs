using ArenaDomain.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaDomain.Entities.Subscription

{
    public class SubscriptionPlan : BaseEntity<Guid>
    {
        public string NameEn { get; set; } = string.Empty;             
        public string NameAr { get; set; } = string.Empty;             
        public string DescriptionEn { get; set; } = string.Empty;      
        public string DescriptionAr { get; set; } = string.Empty;

        public int DurationMonths { get; set; }
        public decimal Price { get; set; }
        public int? SessionLimit { get; set; }

        public bool IsActive { get; set; } = true;
        public bool HasAI { get; set; }

        public decimal? DiscountPercentage { get; set; }
        public DateTime? DiscountEndDate { get; set; }

        public virtual ICollection<UserSubscription> UserSubscriptions { get; set; } = [];
    }
}
