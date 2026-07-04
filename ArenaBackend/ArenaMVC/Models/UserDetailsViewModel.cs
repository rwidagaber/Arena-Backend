using System;
using System.Collections.Generic;
using ArenaDomain.Enums;

namespace ArenaMVC.Models
{
    public class UserDetailsViewModel
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

        // ========== SECTION 1: MEMBERSHIP SUMMARY ==========
        /// <summary>
        /// Membership status enum: User, ActiveMembership, or ExpiredMembership
        /// Based on UserSubscriptions
        /// Views will localize the display
        /// </summary>
        public MembershipStatus MembershipStatus { get; set; } = MembershipStatus.User;

        public bool IsMember { get; set; }
        public DateTime? MemberSince { get; set; }
        public int TotalSubscriptions { get; set; }

        /// <summary>
        /// Current subscription status: Active, Expired, Cancelled, Pending, or null
        /// Views will localize the display
        /// </summary>
        public SubscriptionStatus? CurrentSubscriptionStatus { get; set; } = null;

        // ========== SECTION 2: CURRENT ACTIVE SUBSCRIPTION ==========
        /// <summary>
        /// Current active subscription details (if any)
        /// </summary>
        public SubscriptionItemViewModel? CurrentSubscription { get; set; }

        // ========== SECTION 3: SUBSCRIPTION HISTORY ==========
        /// <summary>
        /// All subscriptions for the user, ordered by CreatedAt DESC
        /// </summary>
        public List<SubscriptionItemViewModel> SubscriptionHistory { get; set; } = new();

        public bool IsManualActive { get; set; }
        public bool IsManualExpiredOrCancelled { get; set; }
    }
}
