using System.ComponentModel.DataAnnotations;

namespace ArenaApplication.Dtos.Payment
{
    public class CreatePaymentDto
    {
        [Required]
        public Guid UserSubscriptionId { get; set; }

        [Required]
        public decimal Amount { get; set; }

        public string Currency { get; set; } = "EGP";

        [Required]
        public string PaymentMethod { get; set; }

        public string? TransactionId { get; set; }
    }
}