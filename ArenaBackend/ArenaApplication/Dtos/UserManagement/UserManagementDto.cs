using System;
using ArenaDomain.Enums;

namespace ArenaApplication.Dtos.UserManagement
{
    public class UserManagementDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime? RegisterDate { get; set; }
        public bool IsActive { get; set; }
        /// <summary>
        /// Membership status derived from UserSubscriptions enum.
        /// Values: User, ActiveMembership, or ExpiredMembership
        /// </summary>
        public MembershipStatus IsMember { get; set; } = MembershipStatus.User;
        /// <summary>
        /// Subscription status: Active, Expired, or null if no subscription
        /// </summary>
        public SubscriptionStatus? SubscriptionStatus { get; set; } = null;
        public bool IsManualActive { get; set; }
        public bool IsManualExpiredOrCancelled { get; set; }
    }
}
