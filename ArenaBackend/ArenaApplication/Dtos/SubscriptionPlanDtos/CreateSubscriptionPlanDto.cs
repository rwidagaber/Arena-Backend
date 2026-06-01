using System.ComponentModel.DataAnnotations;

namespace ArenaApplication.Dtos.SubscriptionPlanDtos
{
    public class CreateSubscriptionPlanDto
    {
        [Required]
        public string Name { get; set; }

        public string DescriptionEnglish { get; set; } = string.Empty;

        [Range(1, 24)]
        public int DurationMonths { get; set; }

        [Range(0.01, 10000)]
        public decimal Price { get; set; }

        public int SessionLimit { get; set; }
    }
}
