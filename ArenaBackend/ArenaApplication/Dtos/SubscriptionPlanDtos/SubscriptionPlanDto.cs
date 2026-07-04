namespace ArenaApplication.Dtos.SubscriptionPlanDtos
{
    public class SubscriptionPlanDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string NameEn { get; set; }
        public string NameAr { get; set; }
        public string DescriptionEn { get; set; }
        public string DescriptionAr { get; set; }
        public int DurationMonths { get; set; }
        public decimal Price { get; set; }
        public int SessionLimit { get; set; }
        public bool IsActive { get; set; }
        public bool HasAI { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public DateTime? DiscountEndDate { get; set; }
        public decimal DiscountedPrice => DiscountPercentage.HasValue && DiscountPercentage.Value > 0 ? Price * (1 - DiscountPercentage.Value / 100) : Price;
        public bool HasDiscount => DiscountPercentage.HasValue && DiscountPercentage.Value > 0;
    }
}
