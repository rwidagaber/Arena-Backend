using ArenaApplication.Dtos.HealthIntelligence;
using ArenaApplication.Dtos.Nutrition;
using ArenaApplication.Dtos.WorkoutPlan;
using ArenaDomain.Entities;
using ArenaDomain.Entities.Bookings;
using ArenaDomain.Entities.Health;
using ArenaDomain.Entities.Nutrition;
using ArenaDomain.Entities.Subscription;
using ArenaDomain.Entities.Workout;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ArenaApplication.AI.Planning
{
    public class GoalAnalysis
    {
        public string PrimaryGoal { get; set; } = string.Empty;
        public string GoalSource { get; set; } = string.Empty; // "Message", "Database", "Fallback"
        public string DurationRequested { get; set; } = string.Empty;
        public bool IsDurationRealistic { get; set; } = true;
        public bool IsDurationPartiallyRealistic { get; set; } = false;
        public string FeasibilityExplanation { get; set; } = string.Empty;
    }

    public class SafetyGuidelines
    {
        public List<string> ExcludedExercises { get; set; } = new();
        public List<string> ExcludedFoods { get; set; } = new();
        public List<string> Substitutions { get; set; } = new();
        public string StrictGuidelinesText { get; set; } = string.Empty;
    }

    public class PlanningContext
    {
        // Inputs
        public Guid MemberProfileId { get; }
        public string UserMessage { get; }
        public string PlanType { get; }
        public bool IsArabic { get; }

        // Extracted DB entities
        public MemberProfile Profile { get; set; } = null!;
        public HealthProfileDto HealthProfile { get; set; } = new();
        public List<ProgressLog> ProgressLogs { get; set; } = new();
        public List<WorkoutPlan> WorkoutPlans { get; set; } = new();
        public List<NutritionPlan> NutritionPlans { get; set; } = new();
        public List<Attendance> Attendances { get; set; } = new();
        public UserSubscription? ActiveSubscription { get; set; }
        
        // Context strings
        public string UserContextText { get; set; } = string.Empty;
        public string MedicalGuidelinesText { get; set; } = string.Empty;

        // AI Message analysis
        public GoalAnalysis GoalInfo { get; set; } = new();
        public SafetyGuidelines SafetyInfo { get; set; } = new();
        public string MessagePreferences { get; set; } = string.Empty;

        // Missing information flag and list of questions
        public bool IsMissingInfo { get; set; }
        public List<string> FollowUpQuestions { get; set; } = new();
        public string ClarificationMessage { get; set; } = string.Empty;

        // Generated results
        public string GeneratedWorkoutPlanJson { get; set; } = string.Empty;
        public string GeneratedNutritionPlanJson { get; set; } = string.Empty;
        public WorkoutPlanDto? WorkoutPlanResult { get; set; }
        public NutritionPlanResponseDto? NutritionPlanResult { get; set; }

        public PlanningContext(Guid memberProfileId, string userMessage, string planType)
        {
            MemberProfileId = memberProfileId;
            UserMessage = userMessage;
            PlanType = planType;
            IsArabic = userMessage != null && userMessage.Any(c => c >= 0x0600 && c <= 0x06FF);
        }
    }
}
