namespace ArenaApplication.Dtos.Payment
{
    public class PaymentDto
    {
        public Guid Id { get; set; }
        public string MemberName { get; set; }
        public Guid MemberId { get; set; }
        public string PlanName { get; set; }

        public Guid PlanId { get; set; }

        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string PaymentMethod { get; set; }
        public string? TransactionId { get; set; }
        public string Status { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string? IframeUrl { get; set; }

        // Subscription info
        public DateTime? SubscriptionEndDate { get; set; }
        public string? SubscriptionStatus { get; set; }
    }
}