namespace ArenaApplication.Dtos.SubscriptionPlanDtos
{
    public class UpdateSubscriptionPlanDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int? DurationMonths { get; set; }

        public decimal? Price { get; set; }
        public int? SessionLimit { get; set; }
        public bool? IsActive { get; set; }
    }
}
