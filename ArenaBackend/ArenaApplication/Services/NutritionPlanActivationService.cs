using ArenaApplication.IServices;
using ArenaDomain.Entities.Nutrition;
using ArenaDomain.Interfaces;
using ArenaDomain.Shared;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ArenaApplication.Services
{
    public class NutritionPlanActivationService : INutritionPlanActivationService
    {
        private readonly IGenericRepository<NutritionPlan, Guid> _planRepo;

        public NutritionPlanActivationService(IGenericRepository<NutritionPlan, Guid> planRepo)
        {
            _planRepo = planRepo;
        }

        public async Task<Result<bool>> ActivateAsync(Guid memberProfileId, Guid planId)
        {
            var plans = await _planRepo.GetAll()
                .Where(p => p.MemberProfileId == memberProfileId && !p.IsDeleted)
                .ToListAsync();

            var target = plans.FirstOrDefault(p => p.Id == planId);
            if (target == null)
                return Result<bool>.Failure("Nutrition plan not found.");

            // Single active plan: turn the target on and the rest off.
            foreach (var plan in plans)
            {
                var shouldBeActive = plan.Id == planId;
                if (plan.IsActive != shouldBeActive)
                {
                    plan.IsActive = shouldBeActive;
                    await _planRepo.UpdateAsync(plan);
                }
            }

            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> DeactivateAsync(Guid memberProfileId, Guid planId)
        {
            var plan = await _planRepo.GetByIdAsync(planId);
            if (plan == null || plan.IsDeleted || plan.MemberProfileId != memberProfileId)
                return Result<bool>.Failure("Nutrition plan not found.");

            if (plan.IsActive)
            {
                plan.IsActive = false;
                await _planRepo.UpdateAsync(plan);
            }

            return Result<bool>.Success(true);
        }
    }
}
