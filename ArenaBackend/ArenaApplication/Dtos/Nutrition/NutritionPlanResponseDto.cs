using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Dtos.Nutrition
{
    public class NutritionPlanResponseDto
    {
        public Guid Id { get; set; }
        public decimal DailyCalories { get; set; }
        public decimal ProteinGrams { get; set; }
        public decimal CarbsGrams { get; set; }
        public decimal FatGrams { get; set; }
        public bool IsActive { get; set; }
        public string AIResponseSummary { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public List<MealResponseDto> Meals { get; set; } = [];
    }
}
