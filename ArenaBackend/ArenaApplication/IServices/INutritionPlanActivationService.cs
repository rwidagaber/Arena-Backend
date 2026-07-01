using ArenaDomain.Shared;
using System;
using System.Threading.Tasks;

namespace ArenaApplication.IServices
{
    public interface INutritionPlanActivationService
    {
        /// <summary>
        /// Activates the given plan for the member and deactivates their other plans
        /// (only one nutrition plan can be active at a time).
        /// </summary>
        Task<Result<bool>> ActivateAsync(Guid memberProfileId, Guid planId);

        /// <summary>Deactivates the given plan if it belongs to the member.</summary>
        Task<Result<bool>> DeactivateAsync(Guid memberProfileId, Guid planId);
    }
}
