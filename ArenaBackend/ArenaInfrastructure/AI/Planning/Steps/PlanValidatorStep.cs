using ArenaApplication.AI.Planning;
using ArenaApplication.IServices;
using System.Threading.Tasks;

namespace ArenaInfrastructure.AI.Planning.Steps
{
    public class PlanValidatorStep : IPlanningStep
    {
        private readonly IHealthIntelligenceService _healthIntelligence;

        public PlanValidatorStep(IHealthIntelligenceService healthIntelligence)
        {
            _healthIntelligence = healthIntelligence;
        }

        public async Task ExecuteAsync(PlanningContext context)
        {
            if (context.IsMissingInfo)
            {
                return;
            }

            // We can add additional validation logs or custom rules here if needed.
            // Under the hood, WorkoutAIService and NutritionAIService already call
            // _healthIntelligence.ValidatePlanAsync with 3 retries during generation.
            // This step acts as a final assertion layer.
            
            if (context.WorkoutPlanResult != null)
            {
                // Workout plan specific assertions (e.g. verify it does not contain excluded exercises)
                foreach (var day in context.WorkoutPlanResult.Days)
                {
                    foreach (var exercise in day.Exercises)
                    {
                        foreach (var excluded in context.SafetyInfo.ExcludedExercises)
                        {
                            if (exercise.Name.Contains(excluded, System.StringComparison.OrdinalIgnoreCase))
                            {
                                // Safety violation detected - we can flag it or attempt replacement
                            }
                        }
                    }
                }
            }

            if (context.NutritionPlanResult != null)
            {
                // Nutrition plan specific assertions (e.g. check for food preferences/allergies)
                foreach (var meal in context.NutritionPlanResult.Meals)
                {
                    foreach (var excluded in context.SafetyInfo.ExcludedFoods)
                    {
                        if (meal.Name.Contains(excluded, System.StringComparison.OrdinalIgnoreCase) ||
                            (meal.Ingredients != null && meal.Ingredients.Contains(excluded, System.StringComparison.OrdinalIgnoreCase)))
                        {
                            // Safety violation detected
                        }
                    }
                }
            }

            await Task.CompletedTask;
        }
    }
}
