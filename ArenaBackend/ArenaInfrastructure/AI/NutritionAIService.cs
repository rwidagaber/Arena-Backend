using ArenaApplication.AI;

using ArenaApplication.Dtos.ChatDtos;
using ArenaApplication.Dtos.Nutrition;
using ArenaApplication.IServices;
using ArenaDomain.Entities.Nutrition;
using ArenaDomain.Enums;
using ArenaInfrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ArenaInfrastructure.AI
{
    public class NutritionPlanAIResponse
    {
        public decimal DailyCalories { get; set; }
        public decimal ProteinGrams { get; set; }
        public decimal CarbsGrams { get; set; }
        public decimal FatGrams { get; set; }
        public List<MealAIResponse> Meals { get; set; } = [];
    }

    public class MealAIResponse
    {
        public string MealType { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Calories { get; set; }
        public decimal ProteinGrams { get; set; }
        public decimal CarbsGrams { get; set; }
        public decimal FatGrams { get; set; }
        public string Ingredients { get; set; } = string.Empty;
    }

    public class NutritionAIService : INutritionAIService
    {
        private readonly IGeminiCompletionService _gemini;
        private readonly AppDbContext _context;
        private readonly IMemberHealthRAGService _healthRAG;

        public NutritionAIService(
            IGeminiCompletionService gemini,
            AppDbContext context,
            IMemberHealthRAGService healthRAG)
        {
            _gemini = gemini;
            _context = context;
            _healthRAG = healthRAG;
        }

        public async Task<NutritionPlanResponseDto> GenerateNutritionPlanAsync(
    Guid memberProfileId, string userMessage)
        {
            var profile = await _context.MemberProfiles
                .FirstOrDefaultAsync(p => p.Id == memberProfileId
                                       || p.UserId == memberProfileId);

            if (profile == null)
                throw new Exception($"MemberProfile not found for Id: {memberProfileId}");

            var effectiveGoal = DetectGoalOverride(userMessage) ?? profile.Goal ?? "General Fitness";
            var goalAwareUserMessage = BuildGoalAwareUserMessage(userMessage, effectiveGoal);
            var healthContext = await _healthRAG.GetRelevantHealthContextAsync(profile.Id, goalAwareUserMessage);
            var recentProgress = await _context.ProgressLogs
                .Where(log => log.MemberProfileId == profile.Id)
                .OrderByDescending(log => log.LoggedAt)
                .Take(8)
                .OrderBy(log => log.LoggedAt)
                .ToListAsync();

            var recentNutritionPlans = await _context.NutritionPlans
                .Where(plan => plan.MemberProfileId == profile.Id && !plan.IsDeleted)
                .Include(plan => plan.Meals)
                .OrderByDescending(plan => plan.CreatedAt)
                .Take(5)
                .ToListAsync();

            var recentWorkoutPlans = await _context.WorkoutPlans
                .Where(plan => plan.MemberProfileId == profile.Id && !plan.IsDeleted)
                .Include(plan => plan.WorkoutDays)
                .OrderByDescending(plan => plan.CreatedAt)
                .Take(5)
                .ToListAsync();

            //  Build subscription for context
            var subscription = await _context.UserSubscriptions
                .FirstOrDefaultAsync(s => s.MemberProfileId == profile.Id
                                       && s.Status == SubscriptionStatus.Active
                                       && s.EndDate > DateTime.UtcNow);

            //  Build userContext
            var userContext = UserContextBuilder.Build(
                profile,
                subscription,
                recentProgress: recentProgress,
                nutritionPlans: recentNutritionPlans,
                workoutPlans: recentWorkoutPlans);

            //  Pass userContext as third parameter
            //var prompt = PromptBuilder.BuildNutritionPrompt(profile, userMessage, userContext);


            var prompt = PromptLoader.GetNutritionPrompt(
    userContext: userContext,
    goal: effectiveGoal,
    dietaryRestrictions: profile.DietaryRestrictions ?? "None",
    healthConditions: profile.HealthConditions ?? "None",
    userMessage: BuildHealthAwareUserMessage(goalAwareUserMessage, healthContext));

            NutritionPlanAIResponse planData;
            try
            {
                var jsonResponse = await _gemini.GetCompletionAsync(
                    prompt, new List<ChatMessageDto>(), "Generate the plan");

                var cleanJson = AIHelper.CleanJson(jsonResponse);
                planData = JsonSerializer.Deserialize<NutritionPlanAIResponse>(
                    cleanJson,
                    CreateJsonOptions()) ?? CreateFallbackPlanData(profile, effectiveGoal);
            }
            catch
            {
                planData = CreateFallbackPlanData(profile, effectiveGoal);
            }

            NormalizeNutritionPlan(planData);

            var activeNutritionPlans = await _context.NutritionPlans
                .Where(existingPlan => existingPlan.MemberProfileId == profile.Id
                    && existingPlan.IsActive
                    && !existingPlan.IsDeleted)
                .ToListAsync();

            foreach (var existingPlan in activeNutritionPlans)
            {
                existingPlan.IsActive = false;
                existingPlan.UpdatedAt = DateTime.UtcNow;
            }

            var plan = new NutritionPlan
            {
                MemberProfileId = profile.Id,
                DailyCalories = planData.DailyCalories,
                ProteinGrams = planData.ProteinGrams,
                CarbsGrams = planData.CarbsGrams,
                FatGrams = planData.FatGrams,
                IsActive = true
            };

            _context.NutritionPlans.Add(plan);
            await _context.SaveChangesAsync();

            var mealDtos = new List<MealResponseDto>();

            foreach (var meal in planData.Meals ?? [])
            {
                var mealEntity = new Meal
                {
                    NutritionPlanId = plan.Id,
                    MealType = meal.MealType,
                    Name = meal.Name,
                    Calories = meal.Calories,
                    Ingredients = meal.Ingredients,
                    Protein = meal.ProteinGrams,
                    Carbs = meal.CarbsGrams,
                    Fat = meal.FatGrams
                };

                _context.Meals.Add(mealEntity);

                mealDtos.Add(new MealResponseDto
                {
                    MealType = meal.MealType,
                    Name = meal.Name,
                    Calories = meal.Calories,
                    ProteinGrams = meal.ProteinGrams,
                    CarbsGrams = meal.CarbsGrams,
                    FatGrams = meal.FatGrams,
                    Ingredients = meal.Ingredients
                });
            }

            await _context.SaveChangesAsync();

            return new NutritionPlanResponseDto
            {
                Id = plan.Id,
                DailyCalories = plan.DailyCalories,
                ProteinGrams = plan.ProteinGrams,
                CarbsGrams = plan.CarbsGrams,
                FatGrams = plan.FatGrams,
                IsActive = plan.IsActive,
                Meals = mealDtos
            };
        }

        private static JsonSerializerOptions CreateJsonOptions()
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            options.Converters.Add(new FlexibleDecimalConverter());
            return options;
        }

        private static void NormalizeNutritionPlan(NutritionPlanAIResponse planData)
        {
            if (planData.DailyCalories <= 0)
                planData.DailyCalories = 2000;

            if (planData.ProteinGrams <= 0)
                planData.ProteinGrams = 120;

            if (planData.CarbsGrams <= 0)
                planData.CarbsGrams = 220;

            if (planData.FatGrams <= 0)
                planData.FatGrams = 65;

            planData.Meals ??= [];

            foreach (var meal in planData.Meals)
            {
                if (string.IsNullOrWhiteSpace(meal.MealType))
                    meal.MealType = "Meal";

                if (string.IsNullOrWhiteSpace(meal.Name))
                    meal.Name = "Balanced meal";

                if (meal.Calories <= 0)
                    meal.Calories = 400;

                if (meal.ProteinGrams <= 0)
                    meal.ProteinGrams = 25;

                if (meal.CarbsGrams <= 0)
                    meal.CarbsGrams = 40;

                if (meal.FatGrams <= 0)
                    meal.FatGrams = 12;

                if (string.IsNullOrWhiteSpace(meal.Ingredients))
                    meal.Ingredients = "Lean protein, complex carbohydrates, vegetables, healthy fats";
            }
        }

        private static NutritionPlanAIResponse CreateFallbackPlanData(ArenaDomain.Entities.MemberProfile profile, string effectiveGoal)
        {
            var weight = Math.Clamp(profile.Weight ?? 70m, 35m, 220m);
            var height = Math.Clamp(profile.Height ?? 170m, 120m, 230m);
            var age = CalculateAge(profile.DateOfBirth);
            var isFemale = profile.Gender == Gender.Female;
            var isWeightLoss = IsWeightLossGoal(profile);
            var isMuscleGain = ContainsAny(profile.Goal, "muscle", "gain", "bulk", "عضلات", "اكسب");
            var hasDiabetes = ContainsAny(profile.HealthConditions, "diabetes", "sugar", "سكري");
            var isVegetarian = ContainsAny(profile.DietaryRestrictions, "vegetarian", "vegan", "نباتي");

            var bmr = 10m * weight + 6.25m * height - 5m * age + (isFemale ? -161m : 5m);
            var maintenance = bmr * GetActivityMultiplier(profile.ActivityLevel);
            var targetGap = profile.TargetWeight.HasValue ? profile.TargetWeight.Value - weight : 0m;

            var calories = maintenance;
            if (isWeightLoss || targetGap < -1m)
                calories -= targetGap < -10m ? 500m : 350m;
            else if (isMuscleGain || targetGap > 1m)
                calories += targetGap > 8m ? 400m : 250m;

            if (hasDiabetes && calories < maintenance - 450m)
                calories = maintenance - 450m;

            calories = Math.Round(Math.Clamp(calories, isFemale ? 1200m : 1400m, 4200m) / 25m) * 25m;
            var proteinMultiplier = isMuscleGain ? 2.0m : isWeightLoss ? 1.8m : 1.6m;
            var protein = Math.Round(weight * proteinMultiplier);
            var fat = Math.Round(Math.Max(weight * 0.7m, calories * 0.22m / 9m));
            var carbs = Math.Round(Math.Max((calories - protein * 4m - fat * 9m) / 4m, hasDiabetes ? 90m : 120m));

            var meals = CreatePersonalizedFallbackMeals(calories, protein, carbs, fat, hasDiabetes, isVegetarian);

            return new NutritionPlanAIResponse
            {
                DailyCalories = calories,
                ProteinGrams = protein,
                CarbsGrams = carbs,
                FatGrams = fat,
                Meals = meals
            };
        }

        private static List<MealAIResponse> CreatePersonalizedFallbackMeals(
            decimal dailyCalories,
            decimal protein,
            decimal carbs,
            decimal fat,
            bool hasDiabetes,
            bool isVegetarian)
        {
            var proteinBase = isVegetarian ? "Greek yogurt, eggs, lentils, chickpeas, tofu" : "eggs, Greek yogurt, chicken, fish";
            var carbBase = hasDiabetes ? "oats, sweet potato, vegetables, brown rice" : "oats, rice, potatoes, fruit, vegetables";
            var breakfastCalories = Math.Round(dailyCalories * 0.25m);
            var lunchCalories = Math.Round(dailyCalories * 0.35m);
            var dinnerCalories = Math.Round(dailyCalories * 0.30m);
            var snackCalories = dailyCalories - breakfastCalories - lunchCalories - dinnerCalories;

            return
            [
                CreateMeal("Breakfast", hasDiabetes ? "Low-GI protein breakfast" : "Protein breakfast", breakfastCalories, protein * 0.25m, carbs * 0.28m, fat * 0.22m, $"{proteinBase}, {carbBase}, chia or nuts"),
                CreateMeal("Lunch", isVegetarian ? "Legume power bowl" : "Lean protein bowl", lunchCalories, protein * 0.35m, carbs * 0.36m, fat * 0.32m, $"{proteinBase}, {carbBase}, salad, olive oil"),
                CreateMeal("Dinner", hasDiabetes ? "Steady blood sugar dinner" : "Recovery dinner", dinnerCalories, protein * 0.30m, carbs * 0.26m, fat * 0.34m, $"{proteinBase}, vegetables, {carbBase}"),
                CreateMeal("Snack", "Goal-support snack", snackCalories, protein * 0.10m, carbs * 0.10m, fat * 0.12m, isVegetarian ? "Cottage cheese or hummus with vegetables" : "Protein shake or yogurt with nuts")
            ];
        }

        private static MealAIResponse CreateMeal(
            string type,
            string name,
            decimal calories,
            decimal protein,
            decimal carbs,
            decimal fat,
            string ingredients) => new()
            {
                MealType = type,
                Name = name,
                Calories = Math.Round(calories),
                ProteinGrams = Math.Round(protein),
                CarbsGrams = Math.Round(carbs),
                FatGrams = Math.Round(fat),
                Ingredients = ingredients
            };

        private static int CalculateAge(DateTime dateOfBirth)
        {
            var today = DateTime.UtcNow.Date;
            var age = today.Year - dateOfBirth.Year;
            if (dateOfBirth.Date > today.AddYears(-age))
                age--;
            return Math.Clamp(age, 13, 90);
        }

        private static decimal GetActivityMultiplier(string? activityLevel)
        {
            if (ContainsAny(activityLevel, "sedentary")) return 1.2m;
            if (ContainsAny(activityLevel, "light")) return 1.375m;
            if (ContainsAny(activityLevel, "active", "very")) return 1.725m;
            if (ContainsAny(activityLevel, "moderate")) return 1.55m;
            return 1.4m;
        }

        private static bool IsWeightLossGoal(ArenaDomain.Entities.MemberProfile profile) =>
            ContainsAny(profile.Goal, "loss", "lose", "cut", "اخس", "تنشيف")
            || (profile.TargetWeight.HasValue && profile.Weight.HasValue && profile.TargetWeight.Value < profile.Weight.Value - 1m);
        private static bool ContainsAny(string? text, params string[] values)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            return values.Any(value => text.Contains(value, StringComparison.OrdinalIgnoreCase));
        }


        private static string? DetectGoalOverride(string? userMessage)
        {
            if (ContainsAny(userMessage, "gain weight", "weight gain", "increase weight", "bulk", "bulking", "gain muscle", "muscle gain", "build muscle", "اكسب وزن", "ازيد وزن", "اضخم", "عضلات"))
                return "Weight Gain / Muscle Gain";

            if (ContainsAny(userMessage, "lose weight", "weight loss", "loss weight", "fat loss", "cut", "cutting", "اخس", "انحف", "تنشيف", "نزل وزن"))
                return "Weight Loss";

            if (ContainsAny(userMessage, "endurance", "fitness", "fit", "لياقة"))
                return "General Fitness";

            return null;
        }

        private static string BuildGoalAwareUserMessage(string userMessage, string effectiveGoal) =>
            $"Current requested goal, if different from profile, is: {effectiveGoal}.\nUser message: {userMessage}";
        private static string BuildHealthAwareUserMessage(string userMessage, string healthContext)
        {
            if (string.IsNullOrWhiteSpace(healthContext))
                return userMessage;

            return $"""
            {userMessage}

            === MEMBER'S KNOWN HEALTH HISTORY (CRITICAL - MUST RESPECT) ===
            {healthContext}
            """;
        }
    }
}

