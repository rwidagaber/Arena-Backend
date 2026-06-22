using System;

namespace ArenaApplication.Dtos.Nutrition
{
    /// <summary>
    /// Snapshot of how much of the user's daily nutrition plan target has been
    /// consumed (from logged meals) and how much is still left for the day.
    /// </summary>
    public class DailyNutritionSummaryDto
    {
        public DateTime Date { get; set; }

        /// <summary>True when the member has an active nutrition plan to track against.</summary>
        public bool HasActivePlan { get; set; }

        public Guid? NutritionPlanId { get; set; }

        // Daily targets coming from the active nutrition plan.
        public decimal DailyCalorieTarget { get; set; }
        public decimal DailyProteinTarget { get; set; }
        public decimal DailyCarbsTarget { get; set; }
        public decimal DailyFatTarget { get; set; }

        // Totals consumed today (sum of today's meal logs).
        public decimal ConsumedCalories { get; set; }
        public decimal ConsumedProtein { get; set; }
        public decimal ConsumedCarbs { get; set; }
        public decimal ConsumedFat { get; set; }

        // What is still left for today (target - consumed). Can be negative when over target.
        public decimal RemainingCalories { get; set; }
        public decimal RemainingProtein { get; set; }
        public decimal RemainingCarbs { get; set; }
        public decimal RemainingFat { get; set; }

        public int MealsLoggedToday { get; set; }

        /// <summary>True once consumed calories exceed the daily target.</summary>
        public bool IsOverTarget { get; set; }

        /// <summary>Human friendly summary of the calories left for today.</summary>
        public string Message { get; set; } = string.Empty;
    }
}
