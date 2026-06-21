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

            var healthContext = await _healthRAG.GetRelevantHealthContextAsync(profile.Id, userMessage);

            // ✅ Build subscription for context
            var subscription = await _context.UserSubscriptions
                .FirstOrDefaultAsync(s => s.MemberProfileId == profile.Id
                                       && s.Status == SubscriptionStatus.Active
                                       && s.EndDate > DateTime.UtcNow);

            // ✅ Build userContext
            var userContext = UserContextBuilder.Build(profile, subscription);

            // ✅ Pass userContext as third parameter
            //var prompt = PromptBuilder.BuildNutritionPrompt(profile, userMessage, userContext);


            var prompt = PromptLoader.GetNutritionPrompt(
    userContext: userContext,
    goal: profile.Goal ?? "General Fitness",
    dietaryRestrictions: profile.DietaryRestrictions ?? "None",
    healthConditions: profile.HealthConditions ?? "None",
    userMessage: BuildHealthAwareUserMessage(userMessage, healthContext));

            NutritionPlanAIResponse planData;
            try
            {
                var jsonResponse = await _gemini.GetCompletionAsync(
                    prompt, new List<ChatMessageDto>(), "Generate the plan");

                var cleanJson = AIHelper.CleanJson(jsonResponse);
                planData = JsonSerializer.Deserialize<NutritionPlanAIResponse>(
                    cleanJson,
                    CreateJsonOptions()) ?? CreateFallbackPlanData(profile);
            }
            catch
            {
                planData = CreateFallbackPlanData(profile);
            }

            NormalizeNutritionPlan(planData);

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

        private static NutritionPlanAIResponse CreateFallbackPlanData(ArenaDomain.Entities.MemberProfile profile)
        {
            var isWeightLoss = ContainsAny(profile.Goal, "loss", "lose", "cut", "اخس", "تنشيف");
            var calories = isWeightLoss ? 1900 : 2400;

            return new NutritionPlanAIResponse
            {
                DailyCalories = calories,
                ProteinGrams = isWeightLoss ? 140 : 170,
                CarbsGrams = isWeightLoss ? 180 : 260,
                FatGrams = isWeightLoss ? 60 : 75,
                Meals =
                [
                    new MealAIResponse
                    {
                        MealType = "Breakfast",
                        Name = "Oats with Greek yogurt",
                        Calories = 450,
                        ProteinGrams = 35,
                        CarbsGrams = 55,
                        FatGrams = 12,
                        Ingredients = "Oats, Greek yogurt, berries, chia seeds"
                    },
                    new MealAIResponse
                    {
                        MealType = "Lunch",
                        Name = "Chicken rice bowl",
                        Calories = 650,
                        ProteinGrams = 50,
                        CarbsGrams = 70,
                        FatGrams = 18,
                        Ingredients = "Grilled chicken, brown rice, vegetables, olive oil"
                    },
                    new MealAIResponse
                    {
                        MealType = "Dinner",
                        Name = "Salmon and sweet potato",
                        Calories = 600,
                        ProteinGrams = 45,
                        CarbsGrams = 50,
                        FatGrams = 24,
                        Ingredients = "Salmon, sweet potato, salad, avocado"
                    },
                    new MealAIResponse
                    {
                        MealType = "Snack",
                        Name = "Protein snack",
                        Calories = 250,
                        ProteinGrams = 25,
                        CarbsGrams = 20,
                        FatGrams = 8,
                        Ingredients = "Protein shake or cottage cheese with fruit"
                    }
                ]
            };
        }

        private static bool ContainsAny(string? text, params string[] values)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            return values.Any(value => text.Contains(value, StringComparison.OrdinalIgnoreCase));
        }

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
