namespace ArenaApplication.Dtos.SubscriptionPlan
{

    public class UpdateSubscriptionPlanDto
    {
        public string? Name { get; set; }
        public decimal? Price { get; set; }
        public int? SessionLimit { get; set; }
        public bool? IsActive { get; set; }
    }

}