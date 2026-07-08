using ArenaApplication.AI.Planning;
using ArenaInfrastructure.AI.Planning.Steps;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArenaInfrastructure.AI.Planning
{
    public class FitnessPlanningPipeline : IFitnessPlanningPipeline
    {
        private readonly AnalyzeUserAndMessageStep _analyzeStep;
        private readonly GoalAndTimeAssessmentStep _goalStep;
        private readonly MedicalSafetyStep _safetyStep;
        private readonly MissingInfoCheckStep _missingStep;
        private readonly PlanGeneratorStep _generatorStep;
        private readonly PlanValidatorStep _validatorStep;

        public FitnessPlanningPipeline(
            AnalyzeUserAndMessageStep analyzeStep,
            GoalAndTimeAssessmentStep goalStep,
            MedicalSafetyStep safetyStep,
            MissingInfoCheckStep missingStep,
            PlanGeneratorStep generatorStep,
            PlanValidatorStep validatorStep)
        {
            _analyzeStep = analyzeStep;
            _goalStep = goalStep;
            _safetyStep = safetyStep;
            _missingStep = missingStep;
            _generatorStep = generatorStep;
            _validatorStep = validatorStep;
        }

        public async Task<PlanningResultDto> ProcessPlanningRequestAsync(Guid memberProfileId, string userMessage, string planType)
        {
            var context = new PlanningContext(memberProfileId, userMessage, planType);

            // Execute steps sequentially
            await _analyzeStep.ExecuteAsync(context);
            await _goalStep.ExecuteAsync(context);
            await _safetyStep.ExecuteAsync(context);
            await _missingStep.ExecuteAsync(context);
            
            // Generate and validate only if all critical info is present
            if (!context.IsMissingInfo)
            {
                await _generatorStep.ExecuteAsync(context);
                await _validatorStep.ExecuteAsync(context);
            }

            return new PlanningResultDto
            {
                IsMissingInfo = context.IsMissingInfo,
                FollowUpQuestions = context.FollowUpQuestions,
                ClarificationMessage = context.ClarificationMessage,
                PlanType = context.PlanType,
                WorkoutPlan = context.WorkoutPlanResult,
                NutritionPlan = context.NutritionPlanResult,
                CoachAnalysis = context.GoalInfo.FeasibilityExplanation
            };
        }
    }
}
