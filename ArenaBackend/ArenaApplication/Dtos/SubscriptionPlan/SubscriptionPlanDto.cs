
namespace ArenaApplication.Dtos.SubscriptionPlan
{
    public class SubscriptionPlanDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int DurationMonths { get; set; }
        public decimal Price { get; set; }
        public int SessionLimit { get; set; }
        public bool IsActive { get; set; }
    }
}

