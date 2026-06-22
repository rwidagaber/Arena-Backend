namespace ArenaApplication.Dtos.Nutrition
{
    /// <summary>
    /// Result returned after the AI analyzes a meal image and the meal is logged
    /// against the member's nutrition plan. Bundles the raw analysis, the saved
    /// meal log, and the updated daily calorie/macro summary.
    /// </summary>
    public class MealAnalysisResultDto
    {
        public MealImageAnalysisDto Analysis { get; set; } = new();

        public MealLogResponseDto LoggedMeal { get; set; } = new();

        public DailyNutritionSummaryDto DailySummary { get; set; } = new();
    }
}
