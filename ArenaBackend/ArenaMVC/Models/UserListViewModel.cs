using System;
using ArenaDomain.Enums;

namespace ArenaMVC.Models
{
    public class UserListViewModel
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime? RegisterDate { get; set; }
        public bool IsActive { get; set; }
        /// <summary>
        /// Membership status enum: User, ActiveMembership, or ExpiredMembership
        /// Based on UserSubscriptions, not MemberProfile existence
        /// Views will localize the display
        /// </summary>
        public MembershipStatus IsMember { get; set; } = MembershipStatus.User;
        /// <summary>
        /// Subscription status: Active, Expired, Cancelled, Pending, or null if no subscription
        /// Views will localize the display
        /// </summary>
        public SubscriptionStatus? SubscriptionStatus { get; set; } = null;
    }
}
