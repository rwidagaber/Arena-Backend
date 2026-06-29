using ArenaApplication.Dtos.WorkoutPlan;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ArenaApplication.IServices
{
    // Workout AI
    public interface IWorkoutAIService
    {
        Task<WorkoutPlanDto> GenerateWorkoutPlanAsync(Guid memberProfileId, string userMessage);

        Task<WorkoutPlanDto> ModifyWorkoutPlanAsync(Guid memberProfileId, string userMessage);
    }
}
