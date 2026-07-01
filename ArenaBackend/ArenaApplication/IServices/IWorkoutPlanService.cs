using ArenaApplication.Dtos.WorkoutPlan;
using ArenaDomain.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArenaApplication.IServices
{
    public interface IWorkoutPlanService
    {
        Task<Result<WorkoutPlanDto>> GetActiveWorkoutPlanByMemberIdAsync(Guid memberProfileId);
        Task<Result<List<WorkoutPlanDto>>> GetWorkoutPlansByMemberIdAsync(Guid memberProfileId);
        Task<Result<WorkoutPlanDto>> GetWorkoutPlanByIdAsync(Guid id, Guid? memberProfileId = null);
        Task<Result<bool>> DeleteWorkoutPlanAsync(Guid id, Guid? memberProfileId = null);
    }
}
