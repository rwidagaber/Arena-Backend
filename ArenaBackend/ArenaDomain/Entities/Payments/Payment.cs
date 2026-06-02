using ArenaDomain.Entities.Subscription;
using ArenaDomain.Entities.User;
using ArenaDomain.Enums;
using ArenaDomain.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaDomain.Entities.Payments
{
    public class Payment : BaseEntity<Guid>
    {
        public Guid UserId { get; set; }

        public virtual ApplicationUser User { get; set; } = null!;

        public Guid? UserSubscriptionId { get; set; }

        public virtual UserSubscription? UserSubscription { get; set; }

        public decimal Amount { get; set; }

        public string Currency { get; set; } = "EGP";

        public PaymentMethod PaymentMethod { get; set; }

        public string? TransactionId { get; set; }

        public string? PaymentIntentId { get; set; }

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        public DateTime? PaymentDate { get; set; }

        public string? FailureReason { get; set; }

        public string? GatewayResponse { get; set; }
    }
}
