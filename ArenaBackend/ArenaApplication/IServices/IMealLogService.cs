using ArenaApplication.Dtos.Nutrition;
using ArenaDomain.Shared;
using System;
using System.Threading.Tasks;

namespace ArenaApplication.IServices
{
    public interface IMealLogService
    {
        /// <summary>
        /// Persists an AI meal-image analysis as a meal log for the member and returns
        /// the saved meal together with the updated daily calorie/macro summary
        /// (target, consumed today, and calories remaining against the active plan).
        /// </summary>
        Task<Result<MealAnalysisResultDto>> LogAnalyzedMealAsync(
            Guid memberProfileId, MealImageAnalysisDto analysis, string? imageUrl = null);

        /// <summary>
        /// Returns how much of the member's daily nutrition target has been consumed
        /// from logged meals on the given date and how much is still left.
        /// </summary>
        Task<Result<DailyNutritionSummaryDto>> GetDailySummaryAsync(
            Guid memberProfileId, DateTime? date = null);
    }
}
