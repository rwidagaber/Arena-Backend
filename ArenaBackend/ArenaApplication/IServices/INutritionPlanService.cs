using ArenaApplication.Dtos.Nutrition;
using ArenaDomain.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArenaApplication.IServices
{
    public interface INutritionPlanService
    {
        Task<Result<NutritionPlanResponseDto>> GetActiveNutritionPlanByMemberIdAsync(Guid memberProfileId);
        Task<Result<List<NutritionPlanResponseDto>>> GetNutritionPlansByMemberIdAsync(Guid memberProfileId);
        Task<Result<NutritionPlanResponseDto>> GetNutritionPlanByIdAsync(Guid id, Guid? memberProfileId = null);
        Task<Result<bool>> DeleteNutritionPlanAsync(Guid id, Guid? memberProfileId = null);
    }
}
