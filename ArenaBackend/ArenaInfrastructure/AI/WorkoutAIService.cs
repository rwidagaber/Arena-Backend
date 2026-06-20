using ArenaApplication.AI;
using ArenaApplication.Dtos.ChatDtos;
using ArenaApplication.Dtos.WorkoutDtos;
using ArenaApplication.Dtos.WorkoutPlan;
using ArenaApplication.IServices;
using ArenaDomain.Entities;
using ArenaDomain.Entities.Subscription;
using ArenaDomain.Entities.Workout;
using ArenaDomain.Enums;
using ArenaDomain.Interfaces;
using ArenaInfrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ArenaInfrastructure.AI
{
    public class WorkoutPlanAIResponse
    {
        public string Name { get; set; } = string.Empty;
        public int DurationWeeks { get; set; }
        public List<WorkoutDayAIResponse> Days { get; set; } = [];
    }

    public class WorkoutDayAIResponse
    {
        public string DayName { get; set; } = string.Empty;
        public List<WorkoutExerciseAIResponse> Exercises { get; set; } = [];
    }

    public class WorkoutExerciseAIResponse
    {
        public string Name { get; set; } = string.Empty;
        public int Sets { get; set; }
        public int Reps { get; set; }
        public string MuscleGroup { get; set; } = string.Empty;
    }

    public class WorkoutAIService : IWorkoutAIService
    {
        private readonly IGeminiCompletionService _gemini;
        private readonly AppDbContext _context;
        private readonly IGenericRepository<MemberProfile, Guid> _memberRepo;
        private readonly IGenericRepository<UserSubscription, Guid> _subscriptionRepo;
        private readonly IMemberHealthRAGService _healthRAG;

        public WorkoutAIService(
            IGeminiCompletionService gemini,
            AppDbContext context,
            IGenericRepository<MemberProfile, Guid> memberProfile,
            IGenericRepository<UserSubscription, Guid> userSubscription,
            IMemberHealthRAGService healthRAG)
        {
            _gemini = gemini;
            _context = context;
            _memberRepo = memberProfile;
            _subscriptionRepo = userSubscription;
            _healthRAG = healthRAG;
        }

        public async Task<WorkoutPlanDto> GenerateWorkoutPlanAsync(Guid memberProfileId, string userMessage)
        {
            // ✅ Search by Id OR UserId
            var profile = await _context.MemberProfiles
                .FirstOrDefaultAsync(p => p.Id == memberProfileId || p.UserId == memberProfileId);

            if (profile == null)
                throw new Exception($"Profile not found: {memberProfileId}");

           

            var healthContext = await _healthRAG.GetRelevantHealthContextAsync(
       profile.Id, userMessage);

            // ✅ OPTIMIZATION: Async database lookup for active subscriptions
            var subscription = await _subscriptionRepo.GetAll()
                .FirstOrDefaultAsync(s => s.MemberProfileId == profile.Id && s.Status == SubscriptionStatus.Active);

            var userContext = UserContextBuilder.Build(profile, subscription);

            var knowledge = GymKnowledge.GetExerciseGuide(
                profile.Goal ?? "General Fitness",
                profile.FitnessExperience ?? "Beginner");

            if (!string.IsNullOrEmpty(profile.Injuries))
                knowledge += GymKnowledge.GetInjuryGuide(profile.Injuries);

            if (!string.IsNullOrEmpty(healthContext))
                knowledge += $"\n\n=== MEMBER'S KNOWN HEALTH HISTORY (CRITICAL — MUST RESPECT) ===\n{healthContext}";


            var prompt = PromptLoader.GetWorkoutPrompt(
                userContext: userContext,
                name: profile.FirstName ?? "User",
                goal: profile.Goal ?? "General Fitness",
                injuries: profile.Injuries ?? "None",
                healthConditions: profile.HealthConditions ?? "None",
                experience: profile.FitnessExperience ?? "Beginner",
                equipment: profile.Equipment ?? "Full Gym",
                userMessage: userMessage + "\n\n" + knowledge);

            WorkoutPlanAIResponse planData;
            try
            {
                var jsonResponse = await _gemini.GetCompletionAsync(prompt, new List<ChatMessageDto>(), "Generate the plan");
                var cleanJson = AIHelper.CleanJson(jsonResponse);
                planData = JsonSerializer.Deserialize<WorkoutPlanAIResponse>(
                    cleanJson,
                    CreateJsonOptions()) ?? CreateFallbackPlanData(profile, userMessage);
            }
            catch
            {
                planData = CreateFallbackPlanData(profile, userMessage);
            }

            NormalizeWorkoutPlan(
                planData,
                profile,
                profile.FirstName ?? "Member",
                userMessage,
                healthContext);

            // ✅ Instantiate Core Plan Entity
            var plan = new WorkoutPlan
            {
                Id = Guid.NewGuid(), // Explicitly setting ID to safely link children before hitting SaveChanges
                MemberProfileId = profile.Id,
                Name = planData.Name,
                DurationWeeks = planData.DurationWeeks,
                IsActive = true
            };
            _context.WorkoutPlans.Add(plan);

            var dayDtos = new List<WorkoutDayDto>();

            // Loop and build out hierarchy in EF Memory State
            foreach (var day in planData.Days ?? [])
            {
                var workoutDay = new WorkoutDay
                {
                    Id = Guid.NewGuid(),
                    WorkoutPlanId = plan.Id,
                    DayName = day.DayName
                };
                _context.WorkoutDays.Add(workoutDay);

                var exerciseDtos = new List<WorkoutExerciseDto>();

                foreach (var ex in day.Exercises ?? [])
                {
                    // Check local change-tracker state cache first, fallback to DB execution context
                    var existingExercise = _context.Exercises.Local
                        .FirstOrDefault(e => e.Name == ex.Name && e.MemberProfileId == profile.Id)
                        ?? await _context.Exercises
                            .FirstOrDefaultAsync(e => e.Name == ex.Name && e.MemberProfileId == profile.Id);

                    if (existingExercise == null)
                    {
                        existingExercise = new Exercise
                        {
                            Id = Guid.NewGuid(),
                            Name = ex.Name,
                            MuscleGroup = ex.MuscleGroup,
                            Description = ex.Name,
                            Equipment = "None",
                            MemberProfileId = profile.Id
                        };
                        _context.Exercises.Add(existingExercise);
                    }

                    _context.WorkoutExercises.Add(new WorkoutExercise
                    {
                        WorkoutDayId = workoutDay.Id,
                        ExerciseId = existingExercise.Id,
                        ExrciseName = ex.Name, // Match this property naming scheme exactly to your entity schema definitions
                        Sets = ex.Sets,
                        Reps = ex.Reps
                    });

                    exerciseDtos.Add(new WorkoutExerciseDto
                    {
                        Name = ex.Name,
                        Sets = ex.Sets,
                        Reps = ex.Reps
                    });
                }

                dayDtos.Add(new WorkoutDayDto
                {
                    Id = workoutDay.Id,
                    WorkoutPlanId = plan.Id,
                    DayName = day.DayName,
                    Exercises = exerciseDtos
                });
            }

            // ✅ OPTIMIZATION: Commit all changes in a single database round-trip transaction block
            await _context.SaveChangesAsync();

            return new WorkoutPlanDto
            {
                Id = plan.Id,
                MemberProfileId = plan.MemberProfileId,
                AssignedTrainerId = plan.AssignedTrainerId,
                Name = plan.Name,
                DurationWeeks = plan.DurationWeeks,
                IsActive = plan.IsActive,
                Days = dayDtos
            };
        }

        private static JsonSerializerOptions CreateJsonOptions()
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            options.Converters.Add(new FlexibleIntConverter());
            return options;
        }

        private static void NormalizeWorkoutPlan(
            WorkoutPlanAIResponse planData,
            MemberProfile profile,
            string memberName,
            string userMessage,
            string healthContext)
        {
            var requestedGoal = DetectGoal(userMessage);
            var requestedDays = DetectRequestedWeeklyFrequency(userMessage);
            var avoidKneeStress = HasKneeIssue(profile, userMessage, healthContext);

            if (!string.IsNullOrWhiteSpace(requestedGoal))
                planData.Name = $"{memberName} {requestedGoal} Workout Plan";
            else if (string.IsNullOrWhiteSpace(planData.Name))
                planData.Name = $"{memberName} Workout Plan";

            if (planData.DurationWeeks <= 0)
                planData.DurationWeeks = 4;

            planData.Days ??= [];

            foreach (var day in planData.Days)
            {
                if (string.IsNullOrWhiteSpace(day.DayName))
                    day.DayName = "Workout Day";

                day.Exercises ??= [];

                foreach (var exercise in day.Exercises)
                {
                    if (string.IsNullOrWhiteSpace(exercise.Name))
                        exercise.Name = "Exercise";

                    if (exercise.Sets <= 0)
                        exercise.Sets = 3;

                    if (exercise.Reps <= 0)
                        exercise.Reps = 10;

                    if (string.IsNullOrWhiteSpace(exercise.MuscleGroup))
                        exercise.MuscleGroup = "General";

                    if (avoidKneeStress && IsKneeStressExercise(exercise.Name))
                        ReplaceWithKneeFriendlyExercise(exercise);
                }
            }

            if (requestedDays.HasValue)
                MatchRequestedFrequency(planData, requestedDays.Value, avoidKneeStress);
        }

        private static WorkoutPlanAIResponse CreateFallbackPlanData(MemberProfile profile, string userMessage = "")
        {
            var memberName = string.IsNullOrWhiteSpace(profile.FirstName) ? "Member" : profile.FirstName;
            var goal = DetectGoal(userMessage)
                ?? (string.IsNullOrWhiteSpace(profile.Goal) ? "General Fitness" : profile.Goal);
            var avoidKneeStress = ContainsAny(profile.Injuries, "knee", "ركبة")
                || ContainsAny(userMessage, "knee", "ركبة");

            return new WorkoutPlanAIResponse
            {
                Name = $"{memberName} {goal} Workout Plan",
                DurationWeeks = 4,
                Days =
                [
                    new WorkoutDayAIResponse
                    {
                        DayName = "Day 1 - Upper Body",
                        Exercises =
                        [
                            new WorkoutExerciseAIResponse { Name = "Chest Press Machine", Sets = 3, Reps = 10, MuscleGroup = "Chest" },
                            new WorkoutExerciseAIResponse { Name = "Lat Pulldown", Sets = 3, Reps = 12, MuscleGroup = "Back" },
                            new WorkoutExerciseAIResponse { Name = "Seated Shoulder Press", Sets = 3, Reps = 10, MuscleGroup = "Shoulders" },
                            new WorkoutExerciseAIResponse { Name = "Cable Row", Sets = 3, Reps = 12, MuscleGroup = "Back" },
                            new WorkoutExerciseAIResponse { Name = "Biceps Curl", Sets = 3, Reps = 12, MuscleGroup = "Arms" },
                            new WorkoutExerciseAIResponse { Name = "Triceps Pushdown", Sets = 3, Reps = 12, MuscleGroup = "Arms" }
                        ]
                    },
                    new WorkoutDayAIResponse
                    {
                        DayName = "Day 2 - Lower Body and Core",
                        Exercises = avoidKneeStress
                            ?
                            [
                                new WorkoutExerciseAIResponse { Name = "Hip Thrust", Sets = 3, Reps = 12, MuscleGroup = "Glutes" },
                                new WorkoutExerciseAIResponse { Name = "Seated Leg Curl", Sets = 3, Reps = 12, MuscleGroup = "Hamstrings" },
                                new WorkoutExerciseAIResponse { Name = "Glute Bridge", Sets = 3, Reps = 15, MuscleGroup = "Glutes" },
                                new WorkoutExerciseAIResponse { Name = "Plank", Sets = 3, Reps = 30, MuscleGroup = "Core" }
                            ]
                            :
                            [
                                new WorkoutExerciseAIResponse { Name = "Leg Press", Sets = 3, Reps = 12, MuscleGroup = "Legs" },
                                new WorkoutExerciseAIResponse { Name = "Romanian Deadlift", Sets = 3, Reps = 10, MuscleGroup = "Hamstrings" },
                                new WorkoutExerciseAIResponse { Name = "Leg Curl", Sets = 3, Reps = 12, MuscleGroup = "Hamstrings" },
                                new WorkoutExerciseAIResponse { Name = "Calf Raise", Sets = 3, Reps = 15, MuscleGroup = "Calves" },
                                new WorkoutExerciseAIResponse { Name = "Plank", Sets = 3, Reps = 30, MuscleGroup = "Core" }
                            ]
                    },
                    new WorkoutDayAIResponse
                    {
                        DayName = "Day 3 - Full Body",
                        Exercises =
                        [
                            new WorkoutExerciseAIResponse { Name = "Dumbbell Bench Press", Sets = 3, Reps = 10, MuscleGroup = "Chest" },
                            new WorkoutExerciseAIResponse { Name = "Assisted Pull-up", Sets = 3, Reps = 8, MuscleGroup = "Back" },
                            new WorkoutExerciseAIResponse { Name = "Cable Face Pull", Sets = 3, Reps = 15, MuscleGroup = "Shoulders" },
                            new WorkoutExerciseAIResponse { Name = "Farmer Carry", Sets = 3, Reps = 30, MuscleGroup = "Full Body" }
                        ]
                    },
                    new WorkoutDayAIResponse
                    {
                        DayName = "Day 4 - Upper Body and Core",
                        Exercises =
                        [
                            new WorkoutExerciseAIResponse { Name = "Incline Chest Press Machine", Sets = 3, Reps = 10, MuscleGroup = "Chest" },
                            new WorkoutExerciseAIResponse { Name = "Seated Cable Row", Sets = 3, Reps = 12, MuscleGroup = "Back" },
                            new WorkoutExerciseAIResponse { Name = "Lateral Raise", Sets = 3, Reps = 12, MuscleGroup = "Shoulders" },
                            new WorkoutExerciseAIResponse { Name = "Cable Wood Chop", Sets = 3, Reps = 12, MuscleGroup = "Core" },
                            new WorkoutExerciseAIResponse { Name = "Dead Bug", Sets = 3, Reps = 12, MuscleGroup = "Core" }
                        ]
                    }
                ]
            };
        }

        private static string? DetectGoal(string? text)
        {
            if (ContainsAny(text, "gain weight", "weight gain", "bulk", "bulking", "gain muscle", "muscle gain", "build muscle", "hypertrophy", "اكسب وزن", "زيادة وزن", "عضلات"))
                return "Weight Gain";

            if (ContainsAny(text, "lose weight", "weight loss", "fat loss", "cutting", "اخس", "انحف", "نزل وزن"))
                return "Weight Loss";

            if (ContainsAny(text, "endurance", "cardio", "stamina", "لياقة"))
                return "Endurance";

            return null;
        }

        private static int? DetectRequestedWeeklyFrequency(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var match = System.Text.RegularExpressions.Regex.Match(
                text,
                @"\b([1-7])\s*(?:days?|times?|sessions?)\s*(?:per\s*)?(?:week|weekly|a\s*week)?\b",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (!match.Success)
                return null;

            return int.TryParse(match.Groups[1].Value, out var days) ? days : null;
        }

        private static bool HasKneeIssue(MemberProfile profile, string? userMessage, string? healthContext) =>
            ContainsAny(profile.Injuries, "knee", "ركبة")
            || ContainsAny(userMessage, "knee", "ركبة")
            || ContainsAny(healthContext, "knee", "ركبة");

        private static bool IsKneeStressExercise(string? exerciseName) =>
            ContainsAny(
                exerciseName,
                "squat",
                "leg press",
                "lunge",
                "running",
                "run",
                "jump",
                "box step",
                "step-up",
                "step up");

        private static void ReplaceWithKneeFriendlyExercise(WorkoutExerciseAIResponse exercise)
        {
            exercise.Name = exercise.MuscleGroup.Contains("leg", StringComparison.OrdinalIgnoreCase)
                || exercise.MuscleGroup.Contains("quad", StringComparison.OrdinalIgnoreCase)
                ? "Seated Leg Curl"
                : "Hip Thrust";
            exercise.Sets = exercise.Sets <= 0 ? 3 : exercise.Sets;
            exercise.Reps = exercise.Reps <= 0 ? 12 : exercise.Reps;
            exercise.MuscleGroup = exercise.Name == "Seated Leg Curl" ? "Hamstrings" : "Glutes";
        }

        private static void MatchRequestedFrequency(
            WorkoutPlanAIResponse planData,
            int requestedDays,
            bool avoidKneeStress)
        {
            requestedDays = Math.Clamp(requestedDays, 1, 7);

            while (planData.Days.Count > requestedDays)
                planData.Days.RemoveAt(planData.Days.Count - 1);

            while (planData.Days.Count < requestedDays)
            {
                var dayNumber = planData.Days.Count + 1;
                planData.Days.Add(new WorkoutDayAIResponse
                {
                    DayName = $"Day {dayNumber} - Upper Body and Core",
                    Exercises = avoidKneeStress
                        ?
                        [
                            new WorkoutExerciseAIResponse { Name = "Chest Press Machine", Sets = 3, Reps = 10, MuscleGroup = "Chest" },
                            new WorkoutExerciseAIResponse { Name = "Lat Pulldown", Sets = 3, Reps = 12, MuscleGroup = "Back" },
                            new WorkoutExerciseAIResponse { Name = "Seated Cable Row", Sets = 3, Reps = 12, MuscleGroup = "Back" },
                            new WorkoutExerciseAIResponse { Name = "Hip Thrust", Sets = 3, Reps = 12, MuscleGroup = "Glutes" },
                            new WorkoutExerciseAIResponse { Name = "Dead Bug", Sets = 3, Reps = 12, MuscleGroup = "Core" }
                        ]
                        :
                        [
                            new WorkoutExerciseAIResponse { Name = "Dumbbell Bench Press", Sets = 3, Reps = 10, MuscleGroup = "Chest" },
                            new WorkoutExerciseAIResponse { Name = "Cable Row", Sets = 3, Reps = 12, MuscleGroup = "Back" },
                            new WorkoutExerciseAIResponse { Name = "Romanian Deadlift", Sets = 3, Reps = 10, MuscleGroup = "Hamstrings" },
                            new WorkoutExerciseAIResponse { Name = "Plank", Sets = 3, Reps = 30, MuscleGroup = "Core" }
                        ]
                });
            }
        }

        private static bool ContainsAny(string? text, params string[] values)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            return values.Any(value => text.Contains(value, StringComparison.OrdinalIgnoreCase));
        }
    }
}
