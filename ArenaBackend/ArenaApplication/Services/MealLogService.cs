using ArenaApplication.Dtos.Nutrition;
using ArenaApplication.IServices;
using ArenaDomain.Entities.Nutrition;
using ArenaDomain.Interfaces;
using ArenaDomain.Shared;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace ArenaApplication.Services
{
    public class MealLogService : IMealLogService
    {
        private const int FoodItemsMaxLength = 2000;
        private const int AICommentMaxLength = 1000;
        private const int ImageUrlMaxLength = 500;

        private readonly IGenericRepository<MealLog, Guid> _mealLogRepo;
        private readonly IGenericRepository<NutritionPlan, Guid> _nutritionPlanRepo;
        private readonly IMapper _mapper;

        public MealLogService(
            IGenericRepository<MealLog, Guid> mealLogRepo,
            IGenericRepository<NutritionPlan, Guid> nutritionPlanRepo,
            IMapper mapper)
        {
            _mealLogRepo = mealLogRepo;
            _nutritionPlanRepo = nutritionPlanRepo;
            _mapper = mapper;
        }

        public async Task<Result<MealAnalysisResultDto>> LogAnalyzedMealAsync(
            Guid memberProfileId, MealImageAnalysisDto analysis, string? imageUrl = null)
        {
            if (analysis == null)
                return Result<MealAnalysisResultDto>.Failure("Meal analysis is required.");

            var foodItems = analysis.DetectedFoods is { Count: > 0 }
                ? string.Join(", ", analysis.DetectedFoods)
                : analysis.MealName;

            var mealLog = new MealLog
            {
                MemberProfileId = memberProfileId,
                ImageUrl = Truncate(imageUrl ?? string.Empty, ImageUrlMaxLength),
                Calories = analysis.EstimatedCalories,
                Protein = analysis.Protein.Grams,
                Carbs = analysis.Carbs.Grams,
                Fat = analysis.Fat.Grams,
                FoodItems = Truncate(foodItems, FoodItemsMaxLength),
                AIComment = Truncate(analysis.Summary, AICommentMaxLength),
                LogDate = DateTime.UtcNow
            };

            await _mealLogRepo.AddAsync(mealLog);

            var summary = await BuildDailySummaryAsync(memberProfileId, mealLog.LogDate);

            var result = new MealAnalysisResultDto
            {
                Analysis = analysis,
                LoggedMeal = _mapper.Map<MealLogResponseDto>(mealLog),
                DailySummary = summary
            };

            return Result<MealAnalysisResultDto>.Success(result);
        }

        public async Task<Result<DailyNutritionSummaryDto>> GetDailySummaryAsync(
            Guid memberProfileId, DateTime? date = null)
        {
            var summary = await BuildDailySummaryAsync(memberProfileId, date ?? DateTime.UtcNow);
            return Result<DailyNutritionSummaryDto>.Success(summary);
        }

        public async Task<Result<DailyNutritionSummaryDto>> DeleteMealLogAsync(
            Guid memberProfileId, Guid mealLogId)
        {
            var mealLog = await _mealLogRepo.GetByIdAsync(mealLogId);

            if (mealLog == null || mealLog.IsDeleted)
                return Result<DailyNutritionSummaryDto>.Failure("Meal log not found.");

            // A member can only remove their own logged meals.
            if (mealLog.MemberProfileId != memberProfileId)
                return Result<DailyNutritionSummaryDto>.Failure(
                    "You are not allowed to delete this meal log.");

            await _mealLogRepo.SoftDeleteAsync(mealLog);

            // Recalculate the day the removed meal belonged to so the caller can
            // refresh its remaining calories/macros after the undo.
            var summary = await BuildDailySummaryAsync(memberProfileId, mealLog.LogDate);
            return Result<DailyNutritionSummaryDto>.Success(summary);
        }

        private async Task<DailyNutritionSummaryDto> BuildDailySummaryAsync(
            Guid memberProfileId, DateTime date)
        {
            var dayStart = date.Date;
            var dayEnd = dayStart.AddDays(1);

            var plan = await _nutritionPlanRepo.GetAll()
                .Where(np => np.MemberProfileId == memberProfileId && np.IsActive && !np.IsDeleted)
                .OrderByDescending(np => np.StartDate)
                .FirstOrDefaultAsync();

            var todaysLogs = await _mealLogRepo.GetAll()
                .Where(ml => ml.MemberProfileId == memberProfileId
                          && !ml.IsDeleted
                          && ml.LogDate >= dayStart
                          && ml.LogDate < dayEnd)
                .ToListAsync();

            var consumedCalories = todaysLogs.Sum(ml => ml.Calories);
            var consumedProtein = todaysLogs.Sum(ml => ml.Protein);
            var consumedCarbs = todaysLogs.Sum(ml => ml.Carbs);
            var consumedFat = todaysLogs.Sum(ml => ml.Fat);

            var summary = new DailyNutritionSummaryDto
            {
                Date = dayStart,
                HasActivePlan = plan != null,
                NutritionPlanId = plan?.Id,
                DailyCalorieTarget = plan?.DailyCalories ?? 0,
                DailyProteinTarget = plan?.ProteinGrams ?? 0,
                DailyCarbsTarget = plan?.CarbsGrams ?? 0,
                DailyFatTarget = plan?.FatGrams ?? 0,
                ConsumedCalories = consumedCalories,
                ConsumedProtein = consumedProtein,
                ConsumedCarbs = consumedCarbs,
                ConsumedFat = consumedFat,
                MealsLoggedToday = todaysLogs.Count
            };

            summary.RemainingCalories = summary.DailyCalorieTarget - consumedCalories;
            summary.RemainingProtein = summary.DailyProteinTarget - consumedProtein;
            summary.RemainingCarbs = summary.DailyCarbsTarget - consumedCarbs;
            summary.RemainingFat = summary.DailyFatTarget - consumedFat;
            summary.IsOverTarget = plan != null && consumedCalories > summary.DailyCalorieTarget;

            summary.Message = BuildMessage(summary);

            return summary;
        }

        private static string BuildMessage(DailyNutritionSummaryDto summary)
        {
            if (!summary.HasActivePlan)
            {
                return "No active nutrition plan yet. Generate a plan to track how many " +
                       "calories you have left each day.";
            }

            var target = Math.Round(summary.DailyCalorieTarget).ToString("0", CultureInfo.InvariantCulture);
            var consumed = Math.Round(summary.ConsumedCalories).ToString("0", CultureInfo.InvariantCulture);

            if (summary.IsOverTarget)
            {
                var over = Math.Round(-summary.RemainingCalories).ToString("0", CultureInfo.InvariantCulture);
                return $"You've eaten {consumed} kcal today and are {over} kcal over your " +
                       $"{target} kcal target.";
            }

            var remaining = Math.Round(summary.RemainingCalories).ToString("0", CultureInfo.InvariantCulture);
            return $"You've eaten {consumed} kcal today. You have {remaining} kcal left of your " +
                   $"{target} kcal target for today.";
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
                return value;

            return value[..maxLength];
        }
    }
}
