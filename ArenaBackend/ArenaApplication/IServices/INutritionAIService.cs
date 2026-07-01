using ArenaApplication.Dtos.Nutrition;

namespace ArenaApplication.IServices
{
    public interface INutritionAIService
    {
        Task<NutritionPlanResponseDto> GenerateNutritionPlanAsync(
            Guid memberProfileId, string userMessage);

        Task<NutritionPlanResponseDto> ModifyNutritionPlanAsync(
            Guid memberProfileId, string userMessage);
    }
}