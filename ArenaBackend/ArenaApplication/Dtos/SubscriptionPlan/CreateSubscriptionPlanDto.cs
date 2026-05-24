
namespace ArenaAPI.DTOs.SubscriptionPlan
{
    public class CreateSubscriptionPlanDto
    {
        [Required]
        public string Name { get; set; }

        [Range(1, 24)]
        public int DurationMonths { get; set; }

        [Range(0.01, 10000)]
        public decimal Price { get; set; }

        public int SessionLimit { get; set; }
    }
}
