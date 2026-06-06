namespace ArenaApplication.Dtos.SubscriptionPlanDtos
{
    public class UpdateSubscriptionPlanDto
    {
        public string? NameEn { get; set; }
        public string? NameAr { get; set; }
        public string? DescriptionEn { get; set; }
        public string? DescriptionAr { get; set; }
        public int? DurationMonths { get; set; }

        public decimal? Price { get; set; }
        public int? SessionLimit { get; set; }
        public bool? IsActive { get; set; }
    }
}
