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
        private readonly INotificationService _notificationService; // ✅

        public WorkoutAIService(
            IGeminiCompletionService gemini,
            AppDbContext context,
            IGenericRepository<MemberProfile, Guid> memberProfile,
            IGenericRepository<UserSubscription, Guid> userSubscription,
            INotificationService notificationService) // ✅
        {
            _gemini = gemini;
            _context = context;
            _memberRepo = memberProfile;
            _subscriptionRepo = userSubscription;
            _notificationService = notificationService; // ✅
        }

        public async Task<WorkoutPlanDto> GenerateWorkoutPlanAsync(Guid memberProfileId, string userMessage)
        {
            var profile = await _context.MemberProfiles
                .FirstOrDefaultAsync(p => p.Id == memberProfileId || p.UserId == memberProfileId);

            if (profile == null)
                throw new Exception($"Profile not found: {memberProfileId}");

            Console.WriteLine($"=== PROFILE ===");
            Console.WriteLine($"Name: {profile.FirstName}");
            Console.WriteLine($"Goal: {profile.Goal}");
            Console.WriteLine($"Injuries: {profile.Injuries}");
            Console.WriteLine($"Experience: {profile.FitnessExperience}");
            Console.WriteLine($"===============");

            var subscription = await _subscriptionRepo.GetAll()
                .FirstOrDefaultAsync(s => s.MemberProfileId == profile.Id
                                       && s.Status == SubscriptionStatus.Active);

            var userContext = UserContextBuilder.Build(profile, subscription);

            var knowledge = GymKnowledge.GetExerciseGuide(
                profile.Goal ?? "General Fitness",
                profile.FitnessExperience ?? "Beginner");

            if (!string.IsNullOrEmpty(profile.Injuries))
                knowledge += GymKnowledge.GetInjuryGuide(profile.Injuries);

            var prompt = PromptLoader.GetWorkoutPrompt(
                userContext: userContext,
                name: profile.FirstName ?? "User",
                goal: profile.Goal ?? "General Fitness",
                injuries: profile.Injuries ?? "None",
                healthConditions: profile.HealthConditions ?? "None",
                experience: profile.FitnessExperience ?? "Beginner",
                equipment: profile.Equipment ?? "Full Gym",
                userMessage: userMessage);

            WorkoutPlanAIResponse planData;
            try
            {
                var jsonResponse = await _gemini.GetCompletionAsync(
                    prompt, new List<ChatMessageDto>(), "Generate the plan");
                var cleanJson = AIHelper.CleanJson(jsonResponse);
                planData = JsonSerializer.Deserialize<WorkoutPlanAIResponse>(
                    cleanJson,
                    CreateJsonOptions()) ?? CreateFallbackPlanData(profile);
            }
            catch
            {
                planData = CreateFallbackPlanData(profile);
            }

            NormalizeWorkoutPlan(planData, profile.FirstName ?? "Member");

            var plan = new WorkoutPlan
            {
                Id = Guid.NewGuid(),
                MemberProfileId = profile.Id,
                Name = planData.Name,
                DurationWeeks = planData.DurationWeeks,
                IsActive = true
            };
            _context.WorkoutPlans.Add(plan);

            var dayDtos = new List<WorkoutDayDto>();

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
                        ExrciseName = ex.Name,
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

            await _context.SaveChangesAsync();

            // ✅ notification إن الـ workout plan اتعمل
            await _notificationService.NotifyWorkoutPlanReadyAsync(profile.Id, plan.Name);

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

        private static void NormalizeWorkoutPlan(WorkoutPlanAIResponse planData, string memberName)
        {
            if (string.IsNullOrWhiteSpace(planData.Name))
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
                    if (string.IsNullOrWhiteSpace(exercise.Name)) exercise.Name = "Exercise";
                    if (exercise.Sets <= 0) exercise.Sets = 3;
                    if (exercise.Reps <= 0) exercise.Reps = 10;
                    if (string.IsNullOrWhiteSpace(exercise.MuscleGroup)) exercise.MuscleGroup = "General";
                }
            }
        }

        private static WorkoutPlanAIResponse CreateFallbackPlanData(MemberProfile profile)
        {
            var memberName = string.IsNullOrWhiteSpace(profile.FirstName) ? "Member" : profile.FirstName;
            var goal = string.IsNullOrWhiteSpace(profile.Goal) ? "General Fitness" : profile.Goal;
            var avoidKneeStress = ContainsAny(profile.Injuries, "knee", "ركبة");

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
                            new WorkoutExerciseAIResponse { Name = "Chest Press Machine",    Sets = 3, Reps = 10, MuscleGroup = "Chest" },
                            new WorkoutExerciseAIResponse { Name = "Lat Pulldown",           Sets = 3, Reps = 12, MuscleGroup = "Back" },
                            new WorkoutExerciseAIResponse { Name = "Seated Shoulder Press",  Sets = 3, Reps = 10, MuscleGroup = "Shoulders" },
                            new WorkoutExerciseAIResponse { Name = "Cable Row",              Sets = 3, Reps = 12, MuscleGroup = "Back" },
                            new WorkoutExerciseAIResponse { Name = "Biceps Curl",            Sets = 3, Reps = 12, MuscleGroup = "Arms" },
                            new WorkoutExerciseAIResponse { Name = "Triceps Pushdown",       Sets = 3, Reps = 12, MuscleGroup = "Arms" }
                        ]
                    },
                    new WorkoutDayAIResponse
                    {
                        DayName = "Day 2 - Lower Body and Core",
                        Exercises = avoidKneeStress
                            ?
                            [
                                new WorkoutExerciseAIResponse { Name = "Hip Thrust",      Sets = 3, Reps = 12, MuscleGroup = "Glutes" },
                                new WorkoutExerciseAIResponse { Name = "Seated Leg Curl", Sets = 3, Reps = 12, MuscleGroup = "Hamstrings" },
                                new WorkoutExerciseAIResponse { Name = "Glute Bridge",    Sets = 3, Reps = 15, MuscleGroup = "Glutes" },
                                new WorkoutExerciseAIResponse { Name = "Plank",           Sets = 3, Reps = 30, MuscleGroup = "Core" }
                            ]
                            :
                            [
                                new WorkoutExerciseAIResponse { Name = "Leg Press",          Sets = 3, Reps = 12, MuscleGroup = "Legs" },
                                new WorkoutExerciseAIResponse { Name = "Romanian Deadlift",  Sets = 3, Reps = 10, MuscleGroup = "Hamstrings" },
                                new WorkoutExerciseAIResponse { Name = "Leg Curl",           Sets = 3, Reps = 12, MuscleGroup = "Hamstrings" },
                                new WorkoutExerciseAIResponse { Name = "Calf Raise",         Sets = 3, Reps = 15, MuscleGroup = "Calves" },
                                new WorkoutExerciseAIResponse { Name = "Plank",              Sets = 3, Reps = 30, MuscleGroup = "Core" }
                            ]
                    },
                    new WorkoutDayAIResponse
                    {
                        DayName = "Day 3 - Full Body",
                        Exercises =
                        [
                            new WorkoutExerciseAIResponse { Name = "Dumbbell Bench Press", Sets = 3, Reps = 10, MuscleGroup = "Chest" },
                            new WorkoutExerciseAIResponse { Name = "Assisted Pull-up",     Sets = 3, Reps = 8,  MuscleGroup = "Back" },
                            new WorkoutExerciseAIResponse { Name = "Cable Face Pull",      Sets = 3, Reps = 15, MuscleGroup = "Shoulders" },
                            new WorkoutExerciseAIResponse { Name = "Farmer Carry",         Sets = 3, Reps = 30, MuscleGroup = "Full Body" }
                        ]
                    }
                ]
            };
        }

        private static bool ContainsAny(string? text, params string[] values)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            return values.Any(v => text.Contains(v, StringComparison.OrdinalIgnoreCase));
        }
    }
}