using ArenaApplication.AI;

using ArenaApplication.Dtos.ChatDtos;
using ArenaApplication.Dtos.Nutrition;
using ArenaApplication.IServices;
using ArenaDomain.Entities.Nutrition;
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
        private readonly IOpenAIService _openAI;
        private readonly AppDbContext _context;

        public NutritionAIService(IOpenAIService openAI, AppDbContext context)
        {
            _openAI = openAI;
            _context = context;
        }

        public async Task<NutritionPlanResponseDto> GenerateNutritionPlanAsync(
            Guid memberProfileId, string userMessage)
        {
            var profile = await _context.MemberProfiles
                .FirstOrDefaultAsync(p => p.Id == memberProfileId
                                       || p.UserId == memberProfileId);

            if (profile == null)
                throw new Exception($"MemberProfile not found for Id: {memberProfileId}");

            var prompt = PromptBuilder.BuildNutritionPrompt(profile, userMessage);

            var jsonResponse = await _openAI.GetCompletionAsync(
                prompt, new List<ChatMessageDto>(), "Generate the plan");

            Console.WriteLine("=== NUTRITION RAW ===");
            Console.WriteLine(jsonResponse);
            Console.WriteLine("=====================");

            var cleanJson = AIHelper.CleanJson(jsonResponse);

            var planData = JsonSerializer.Deserialize<NutritionPlanAIResponse>(
                cleanJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (planData == null)
                throw new Exception("AI returned invalid nutrition plan JSON");

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
                    //ProteinGrams = meal.ProteinGrams,
                    //CarbsGrams = meal.CarbsGrams,
                    //FatGrams = meal.FatGrams,
                    Ingredients = meal.Ingredients
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
    }
}