namespace ArenaAPI.DTOs.Payment
{
    public class PaymentDto
    {
        public int Id { get; set; }
        public string MemberName { get; set; }
        public string PlanName { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string PaymentMethod { get; set; }
        public string? TransactionId { get; set; }
        public string Status { get; set; }
        public DateTime PaymentDate { get; set; }
    }
}