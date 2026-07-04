using System;
using System.Collections.Generic;
using ArenaDomain.Enums;

namespace ArenaApplication.Dtos.UserManagement
{
    public class UserManagementDetailsDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string PreferredLanguage { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public DateTime? RegisterDate { get; set; }

        // Membership Info
        /// <summary>
        /// Membership status derived from UserSubscriptions enum.
        /// Values: User, ActiveMembership, or ExpiredMembership
        /// </summary>
        public MembershipStatus MembershipStatus { get; set; } = MembershipStatus.User;
        public bool IsMember { get; set; }
        public DateTime? MemberSince { get; set; }
        public int TotalSubscriptions { get; set; }
        /// <summary>
        /// Current subscription status: Active, Expired, Cancelled, Pending, or null
        /// </summary>
        public SubscriptionStatus? CurrentSubscriptionStatus { get; set; } = null;

        // Current Subscription
        public UserSubscriptionItemDto? CurrentSubscription { get; set; }

        // Subscription History
        public List<UserSubscriptionItemDto> SubscriptionHistory { get; set; } = new();

        // Manual subscription management properties
        public bool HasActiveSubscription { get; set; }
        public Guid? CurrentSubscriptionId { get; set; }
        public string? CurrentPlanNameEn { get; set; }
        public string? CurrentPlanNameAr { get; set; }
        public bool IsManualActive { get; set; }
        public bool IsManualExpiredOrCancelled { get; set; }
        public List<SubscriptionPlanSelectionDto> AvailablePlans { get; set; } = new();
    }

    public class SubscriptionPlanSelectionDto
    {
        public Guid Id { get; set; }
        public string NameEn { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int DurationMonths { get; set; }
        public bool HasAI { get; set; }
    }
}
