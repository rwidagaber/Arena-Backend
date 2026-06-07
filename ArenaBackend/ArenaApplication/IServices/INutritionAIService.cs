using ArenaApplication.Dtos.Nutrition;

namespace ArenaApplication.IServices
{
    public interface INutritionAIService
    {
        Task<NutritionPlanResponseDto> GenerateNutritionPlanAsync(
            Guid memberProfileId, string userMessage);
    }
}