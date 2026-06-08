using ArenaApplication.Dtos.WorkoutPlan;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.IServices
{
    // Workout AI
    public interface IWorkoutAIService
    {
        //Task<WorkoutPlanDto> GenerateWorkoutPlanAsync(Guid memberProfileId);
        Task<WorkoutPlanDto> GenerateWorkoutPlanAsync(
    Guid memberProfileId, string userMessage);
    }
}
