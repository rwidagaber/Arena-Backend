using ArenaDomain.Enums;
using System.ComponentModel.DataAnnotations;

namespace ArenaApplication.Dtos.Payment
{
    public class CreatePaymentDto
    {
        [Required]
        public Guid PlanId { get; set; }

        [Required]
        public PaymentMethod PaymentMethod { get; set; }

        public string Currency { get; set; } = "EGP";
    }
}