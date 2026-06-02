namespace ArenaApplication.Dtos.SubscriptionPlanDtos
{
    public class SubscriptionPlanDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int DurationMonths { get; set; }
        public decimal Price { get; set; }
        public int SessionLimit { get; set; }
        public bool IsActive { get; set; }
    }
}
