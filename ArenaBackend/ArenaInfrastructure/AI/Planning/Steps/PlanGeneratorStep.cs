using ArenaApplication.AI.Planning;
using ArenaApplication.IServices;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArenaInfrastructure.AI.Planning.Steps
{
    public class PlanGeneratorStep : IPlanningStep
    {
        private readonly IWorkoutAIService _workoutAI;
        private readonly INutritionAIService _nutritionAI;

        public PlanGeneratorStep(IWorkoutAIService workoutAI, INutritionAIService nutritionAI)
        {
            _workoutAI = workoutAI;
            _nutritionAI = nutritionAI;
        }

        public async Task ExecuteAsync(PlanningContext context)
        {
            if (context.IsMissingInfo)
            {
                return;
            }

            // Enhance user message with the feasibility assessment and medical safety details
            var enhancedMessage = context.UserMessage;
            var prependedInstructions = new List<string>();

            if (!string.IsNullOrWhiteSpace(context.GoalInfo.FeasibilityExplanation))
            {
                prependedInstructions.Add($"[COACH FEASIBILITY ANALYSIS - MUST RESPECT & ADDRESS]: {context.GoalInfo.FeasibilityExplanation}");
            }

            if (context.SafetyInfo.ExcludedExercises.Any() || context.SafetyInfo.ExcludedFoods.Any())
            {
                var medicalSafetyText = "[MEDICAL SAFETY INSTRUCTIONS - CRITICAL]:\n";
                if (context.SafetyInfo.ExcludedExercises.Any())
                {
                    medicalSafetyText += $"- EXCLUDE THESE EXERCISES: {string.Join(", ", context.SafetyInfo.ExcludedExercises)}\n";
                }
                if (context.SafetyInfo.ExcludedFoods.Any())
                {
                    medicalSafetyText += $"- EXCLUDE THESE FOODS/INGREDIENTS: {string.Join(", ", context.SafetyInfo.ExcludedFoods)}\n";
                }
                if (context.SafetyInfo.Substitutions.Any())
                {
                    medicalSafetyText += $"- USE THESE SAFE SUBSTITUTIONS: {string.Join(", ", context.SafetyInfo.Substitutions)}\n";
                }
                prependedInstructions.Add(medicalSafetyText);
            }

            if (prependedInstructions.Any())
            {
                enhancedMessage = string.Join("\n\n", prependedInstructions) + $"\n\nUser message: {context.UserMessage}";
            }

            if (context.PlanType == "workout" || context.PlanType == "both")
            {
                context.WorkoutPlanResult = await _workoutAI.GenerateWorkoutPlanAsync(context.Profile.Id, enhancedMessage);
            }

            if (context.PlanType == "nutrition" || context.PlanType == "both")
            {
                context.NutritionPlanResult = await _nutritionAI.GenerateNutritionPlanAsync(context.Profile.Id, enhancedMessage);
            }
        }
    }
}
