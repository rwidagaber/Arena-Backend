using ArenaApplication.Dtos.Nutrition;
using ArenaApplication.Dtos.WorkoutPlan;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArenaApplication.AI.Planning
{
    public class PlanningResultDto
    {
        public bool IsMissingInfo { get; set; }
        public List<string> FollowUpQuestions { get; set; } = new();
        public string ClarificationMessage { get; set; } = string.Empty;
        public string PlanType { get; set; } = string.Empty; // "workout", "nutrition", "both"
        public WorkoutPlanDto? WorkoutPlan { get; set; }
        public NutritionPlanResponseDto? NutritionPlan { get; set; }
        public string CoachAnalysis { get; set; } = string.Empty;
    }

    public interface IFitnessPlanningPipeline
    {
        Task<PlanningResultDto> ProcessPlanningRequestAsync(Guid memberProfileId, string userMessage, string planType);
    }
}
