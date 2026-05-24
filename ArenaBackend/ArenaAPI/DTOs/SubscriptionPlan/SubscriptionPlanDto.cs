
namespace ArenaAPI.DTOs.SubscriptionPlan
{
    public class SubscriptionPlanDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int DurationMonths { get; set; }
        public decimal Price { get; set; }
        public int SessionLimit { get; set; }
        public bool IsActive { get; set; }
    }
}

