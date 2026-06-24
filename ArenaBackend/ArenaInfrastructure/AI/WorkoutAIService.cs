using ArenaApplication.AI;
using ArenaApplication.Dtos.ChatDtos;
using ArenaApplication.Dtos.WorkoutDtos;
using ArenaApplication.Dtos.WorkoutPlan;
using ArenaApplication.Dtos.HealthIntelligence;
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
        private readonly INotificationService _notificationService; // ✅
        private readonly IHealthIntelligenceService _healthIntelligence;

        public WorkoutAIService(
            IGeminiCompletionService gemini,
            AppDbContext context,
            IGenericRepository<MemberProfile, Guid> memberProfile,
            IGenericRepository<UserSubscription, Guid> userSubscription,
            IMemberHealthRAGService healthRAG,
            INotificationService notificationService,
            IHealthIntelligenceService healthIntelligence) // ✅
        {
            _gemini = gemini;
            _context = context;
            _memberRepo = memberProfile;
            _subscriptionRepo = userSubscription;
            _healthRAG = healthRAG;
            _notificationService = notificationService; // ✅
            _healthIntelligence = healthIntelligence;
        }

        public async Task<WorkoutPlanDto> GenerateWorkoutPlanAsync(Guid memberProfileId, string userMessage)
        {
            // ✅ Search by Id OR UserId
            var profile = await _context.MemberProfiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == memberProfileId || p.UserId == memberProfileId);

            if (profile == null)
                throw new Exception($"Profile not found: {memberProfileId}");

            Console.WriteLine($"=== PROFILE ===");
            Console.WriteLine($"Name: {profile.FirstName}");
            Console.WriteLine($"Goal: {profile.Goal}");
            Console.WriteLine($"Injuries: {profile.Injuries}");
            Console.WriteLine($"Experience: {profile.FitnessExperience}");
            Console.WriteLine($"===============");

            var effectiveGoal = DetectGoalOverride(userMessage) ?? profile.Goal ?? "General Fitness";

            var goalOverride = DetectGoalOverride(userMessage);
            if (goalOverride != null && !string.Equals(profile.Goal, goalOverride, StringComparison.OrdinalIgnoreCase))
            {
                profile.Goal = goalOverride;
                _context.MemberProfiles.Update(profile);
            }

            var memberName = GetMemberName(profile);
            var goalAwareUserMessage = BuildGoalAwareUserMessage(userMessage, effectiveGoal);
            var healthContext = await _healthRAG.GetRelevantHealthContextAsync(profile.Id, goalAwareUserMessage);
            var recentProgress = await _context.ProgressLogs
                .Where(log => log.MemberProfileId == profile.Id)
                .OrderByDescending(log => log.LoggedAt)
                .Take(8)
                .OrderBy(log => log.LoggedAt)
                .ToListAsync();

            var recentNutritionPlans = await _context.NutritionPlans
                .Where(plan => plan.MemberProfileId == profile.Id && !plan.IsDeleted)
                .Include(plan => plan.Meals)
                .OrderByDescending(plan => plan.CreatedAt)
                .Take(5)
                .ToListAsync();

            var recentWorkoutPlans = await _context.WorkoutPlans
                .Where(plan => plan.MemberProfileId == profile.Id && !plan.IsDeleted)
                .Include(plan => plan.WorkoutDays)
                .OrderByDescending(plan => plan.CreatedAt)
                .Take(5)
                .ToListAsync();

            // ✅ OPTIMIZATION: Async database lookup for active subscriptions
            var subscription = await _subscriptionRepo.GetAll()
                .FirstOrDefaultAsync(s => s.MemberProfileId == profile.Id && s.Status == SubscriptionStatus.Active);

            var userContext = UserContextBuilder.Build(
                profile,
                subscription,
                recentProgress: recentProgress,
                nutritionPlans: recentNutritionPlans,
                workoutPlans: recentWorkoutPlans);

            var knowledge = GymKnowledge.GetExerciseGuide(
                effectiveGoal,
                profile.FitnessExperience ?? "Beginner");

            if (!string.IsNullOrEmpty(profile.Injuries))
                knowledge += GymKnowledge.GetInjuryGuide(profile.Injuries);

            if (!string.IsNullOrEmpty(healthContext))
                knowledge += $"\n\n=== MEMBER'S KNOWN HEALTH HISTORY (CRITICAL - MUST RESPECT) ===\n{healthContext}";

            HealthProfileDto healthProfile = new HealthProfileDto();
            if (!string.IsNullOrWhiteSpace(profile.HealthProfileJson))
            {
                healthProfile = System.Text.Json.JsonSerializer.Deserialize<HealthProfileDto>(profile.HealthProfileJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new HealthProfileDto();
            }

            var medicalGuidelines = await _healthIntelligence.RetrieveMedicalGuidelinesAsync(healthProfile);
            if (!string.IsNullOrWhiteSpace(medicalGuidelines))
            {
                knowledge += $"\n\n=== STRICT MEDICAL GUIDELINES (WHO/CDC/NHS) ===\n{medicalGuidelines}";
            }

            var availableEquipments = await _context.Equipments
                .Where(e => e.IsAvailable)
                .Select(e => e.Name)
                .ToListAsync();
            var equipmentStr = string.Join(", ", availableEquipments);
            if (string.IsNullOrEmpty(equipmentStr)) equipmentStr = "Bodyweight only";

            var catalogItems = await _context.ExerciseCatalogItems
                .Include(c => c.EquipmentRequirements)
                .ThenInclude(er => er.Equipment)
                .ToListAsync();

            var validCatalogItems = catalogItems.Where(c => c.EquipmentRequirements.All(er => er.Equipment.IsAvailable)).ToList();
            var exerciseCatalogStr = string.Join("\n", validCatalogItems.Select(c => $"- {c.Name} ({c.MuscleGroup})"));

            var prompt = PromptLoader.GetWorkoutPrompt(
                userContext: userContext,
                name: profile.FirstName ?? "User",
                goal: effectiveGoal,
                injuries: profile.Injuries ?? "None",
                healthConditions: profile.HealthConditions ?? "None",
                experience: profile.FitnessExperience ?? "Beginner",
                equipment: equipmentStr,
                exerciseCatalog: exerciseCatalogStr,
                userMessage: goalAwareUserMessage + "\n\n" + knowledge);

            WorkoutPlanAIResponse planData = null;
            int retries = 0;
            bool isValid = false;
            string currentPrompt = prompt;

            while (retries < 3 && !isValid)
            {
                try
                {
                    var jsonResponse = await _gemini.GetCompletionAsync(currentPrompt, new List<ChatMessageDto>(), "Generate the plan");
                    var cleanJson = AIHelper.CleanJson(jsonResponse);
                    planData = JsonSerializer.Deserialize<WorkoutPlanAIResponse>(
                        cleanJson,
                        CreateJsonOptions()) ?? CreateFallbackPlanData(profile, goalAwareUserMessage, effectiveGoal, memberName);

                    var validationResult = await _healthIntelligence.ValidatePlanAsync(healthProfile, cleanJson, "Workout");
                    
                    if (validationResult.IsValid)
                    {
                        isValid = true;
                    }
                    else
                    {
                        retries++;
                        currentPrompt = prompt + $"\n\n[CRITICAL FEEDBACK - REGENERATION REQUIRED]: Your previous plan was REJECTED by the Medical Validation Layer for the following reason:\n{validationResult.RejectionReason}\nYou MUST fix this immediately and provide a new, safe plan.";
                    }
                }
                catch
                {
                    retries++;
                }
            }

            if (!isValid || planData == null)
            {
                planData = CreateFallbackPlanData(profile, goalAwareUserMessage, effectiveGoal, memberName);
            }

            NormalizeWorkoutPlan(planData, profile, memberName, goalAwareUserMessage, healthContext, effectiveGoal);
            ApplyEquipmentSubstitution(planData, catalogItems, validCatalogItems);

            var activeWorkoutPlans = await _context.WorkoutPlans
                .Where(existingPlan => existingPlan.MemberProfileId == profile.Id
                    && existingPlan.IsActive
                    && !existingPlan.IsDeleted)
                .ToListAsync();

            foreach (var existingPlan in activeWorkoutPlans)
            {
                existingPlan.IsActive = false;
                existingPlan.UpdatedAt = DateTime.UtcNow;
            }

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
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };
            options.Converters.Add(new FlexibleIntConverter());
            return options;
        }

        private static void NormalizeWorkoutPlan(
            WorkoutPlanAIResponse planData,
            MemberProfile profile,
            string memberName,
            string userMessage,
            string healthContext,
            string effectiveGoal)
        {
            if (string.IsNullOrWhiteSpace(planData.Name))
                planData.Name = $"{memberName} Workout Plan";

            if (planData.DurationWeeks <= 0)
                planData.DurationWeeks = 4;

            planData.Days ??= [];
            var avoidKneeStress = ContainsAny(profile.Injuries, "knee", "ركبة")
                || ContainsAny(userMessage, "knee", "ركبة")
                || ContainsAny(healthContext, "knee", "ركبة");

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
        }

        private static void ApplyEquipmentSubstitution(WorkoutPlanAIResponse planData, List<ExerciseCatalogItem> fullCatalog, List<ExerciseCatalogItem> validCatalog)
        {
            if (planData.Days == null) return;

            foreach (var day in planData.Days)
            {
                if (day.Exercises == null) continue;

                foreach (var ex in day.Exercises)
                {
                    var matchedCatalog = fullCatalog.FirstOrDefault(c => c.Name.Equals(ex.Name, StringComparison.OrdinalIgnoreCase));

                    if (matchedCatalog != null && !validCatalog.Contains(matchedCatalog))
                    {
                        // Needs substitution! Requires unavailable equipment
                        var substitute = validCatalog.FirstOrDefault(c => c.MuscleGroup.Equals(matchedCatalog.MuscleGroup, StringComparison.OrdinalIgnoreCase)) ?? validCatalog.FirstOrDefault();
                        if (substitute != null)
                        {
                            ex.Name = substitute.Name;
                            ex.MuscleGroup = substitute.MuscleGroup;
                        }
                    }
                    else if (matchedCatalog == null)
                    {
                        // AI generated an exercise not in the catalog. 
                        // Strictly enforce valid catalog to guarantee equipment availability.
                        var substitute = validCatalog.FirstOrDefault(c => c.MuscleGroup.Equals(ex.MuscleGroup, StringComparison.OrdinalIgnoreCase)) ?? validCatalog.FirstOrDefault();
                        if (substitute != null)
                        {
                            ex.Name = substitute.Name;
                            ex.MuscleGroup = substitute.MuscleGroup;
                        }
                    }
                }
            }
        }

        private static WorkoutPlanAIResponse CreateFallbackPlanData(MemberProfile profile, string userMessage, string effectiveGoal, string memberName)
        {
            var goal = effectiveGoal;
            var defaults = GetTrainingDefaults(profile, effectiveGoal);
            var avoidKneeStress = ContainsAny(profile.Injuries, "knee", "ركبة")
                || ContainsAny(userMessage, "knee", "ركبة");
            var avoidBackStress = ContainsAny(profile.Injuries, "back", "spine", "lower back", "ظهر")
                || ContainsAny(userMessage, "back", "spine", "lower back", "ظهر");
            var avoidArmStress = ContainsAny(profile.Injuries, "arm", "shoulder", "elbow", "wrist", "ذراع", "كتف")
                || ContainsAny(userMessage, "arm", "shoulder", "elbow", "wrist", "ذراع", "كتف");
            var hasDiabetes = ContainsAny(profile.HealthConditions, "diabetes", "sugar", "سكري");

            var days = new List<WorkoutDayAIResponse>
            {
                new()
                {
                    DayName = "Day 1 - Controlled Upper Body",
                    Exercises = avoidArmStress
                        ?
                        [
                            Exercise("Walking or Bike Warmup", 1, hasDiabetes ? 12 : 10, "Cardio"),
                            Exercise("Scapular Retraction", defaults.Sets, defaults.Reps + 2, "Posture"),
                            Exercise("Cable Row Light", defaults.Sets, defaults.Reps, "Back"),
                            Exercise("Wall Push-up", defaults.Sets, defaults.Reps, "Chest"),
                            Exercise("Dead Bug", defaults.Sets, 10, "Core")
                        ]
                        :
                        [
                            Exercise("Chest Press Machine", defaults.Sets, defaults.Reps, "Chest"),
                            Exercise("Lat Pulldown", defaults.Sets, defaults.Reps + 2, "Back"),
                            Exercise("Seated Shoulder Press", Math.Max(defaults.Sets - 1, 2), defaults.Reps, "Shoulders"),
                            Exercise("Cable Row", defaults.Sets, defaults.Reps + 2, "Back"),
                            Exercise("Biceps Curl", Math.Max(defaults.Sets - 1, 2), defaults.Reps + 2, "Arms"),
                            Exercise("Triceps Pushdown", Math.Max(defaults.Sets - 1, 2), defaults.Reps + 2, "Arms")
                        ]
                },
                new()
                {
                    DayName = "Day 2 - Joint-Friendly Lower Body and Core",
                    Exercises = avoidKneeStress
                        ?
                        [
                            Exercise("Hip Thrust", defaults.Sets, defaults.Reps + 2, "Glutes"),
                            Exercise("Seated Leg Curl", defaults.Sets, defaults.Reps + 2, "Hamstrings"),
                            Exercise("Glute Bridge", defaults.Sets, defaults.Reps + 4, "Glutes"),
                            Exercise(avoidBackStress ? "Bird Dog" : "Plank", defaults.Sets, avoidBackStress ? 10 : 30, "Core"),
                            Exercise("Bike Moderate Pace", 1, hasDiabetes ? 15 : 10, "Cardio")
                        ]
                        :
                        [
                            Exercise(avoidBackStress ? "Leg Extension Machine" : "Leg Press", defaults.Sets, defaults.Reps + 2, "Legs"),
                            Exercise("Seated Leg Curl", defaults.Sets, defaults.Reps + 2, "Hamstrings"),
                            Exercise("Calf Raise", defaults.Sets, defaults.Reps + 4, "Calves"),
                            Exercise(avoidBackStress ? "Dead Bug" : "Plank", defaults.Sets, avoidBackStress ? 10 : 30, "Core"),
                            Exercise("Incline Walk", 1, hasDiabetes ? 15 : 10, "Cardio")
                        ]
                }
            };

            if (defaults.WeeklyDays >= 3)
            {
                days.Add(new WorkoutDayAIResponse
                {
                    DayName = "Day 3 - Full Body Technique",
                    Exercises =
                    [
                        Exercise(avoidBackStress ? "Machine Chest Press" : "Dumbbell Bench Press", defaults.Sets, defaults.Reps, "Chest"),
                        Exercise("Assisted Pull-up", Math.Max(defaults.Sets - 1, 2), Math.Max(defaults.Reps - 2, 6), "Back"),
                        Exercise("Cable Face Pull", defaults.Sets, defaults.Reps + 4, "Shoulders"),
                        Exercise(avoidBackStress ? "Suitcase Hold" : "Farmer Carry", Math.Max(defaults.Sets - 1, 2), 30, "Full Body")
                    ]
                });
            }

            if (defaults.WeeklyDays >= 4)
            {
                days.Add(new WorkoutDayAIResponse
                {
                    DayName = hasDiabetes ? "Day 4 - Moderate Cardio and Mobility" : "Day 4 - Goal Conditioning",
                    Exercises =
                    [
                        Exercise("Bike Moderate Pace", 1, hasDiabetes ? 20 : 15, "Cardio"),
                        Exercise("Mobility Flow", 1, 10, "Mobility"),
                        Exercise("Cable Row Light", 2, defaults.Reps + 2, "Back"),
                        Exercise("Glute Bridge", 2, defaults.Reps + 4, "Glutes")
                    ]
                });
            }

            return new WorkoutPlanAIResponse
            {
                Name = $"{memberName} {goal} Personalized Workout Plan",
                DurationWeeks = defaults.DurationWeeks,
                Days = days
            };
        }

        private static WorkoutExerciseAIResponse Exercise(string name, int sets, int reps, string muscleGroup) => new()
        {
            Name = name,
            Sets = sets,
            Reps = reps,
            MuscleGroup = muscleGroup
        };

        private static (int Sets, int Reps, int WeeklyDays, int DurationWeeks) GetTrainingDefaults(MemberProfile profile, string effectiveGoal)
        {
            var isBeginner = ContainsAny(profile.FitnessExperience, "beginner", "new", "مبتدئ");
            var isAdvanced = ContainsAny(profile.FitnessExperience, "advanced", "متقدم");
            var hasPainOrDisease = !string.IsNullOrWhiteSpace(profile.Injuries)
                || ContainsAny(profile.HealthConditions, "diabetes", "heart", "pressure", "سكري", "ضغط");
            var isWeightLoss = ContainsAny(effectiveGoal, "loss", "lose", "cut", "اخس", "تنشيف")
                || (profile.TargetWeight.HasValue && profile.Weight.HasValue && profile.TargetWeight.Value < profile.Weight.Value - 1m);
            var isGain = ContainsAny(effectiveGoal, "gain", "bulk", "muscle", "اكسب", "اضخم", "عضلات");

            var sets = isBeginner || hasPainOrDisease ? 2 : isAdvanced ? 4 : 3;
            var reps = isWeightLoss ? 14 : isGain ? 8 : isAdvanced ? 8 : 10;
            var weeklyDays = hasPainOrDisease ? 3 : isAdvanced ? 4 : 3;
            var durationWeeks = isBeginner || hasPainOrDisease ? 6 : 4;

            return (sets, reps, weeklyDays, durationWeeks);
        }

        private static string? DetectGoalOverride(string? userMessage)
        {
            if (ContainsAny(userMessage, "gain weight", "weight gain", "increase weight", "bulk", "bulking", "gain muscle", "muscle gain", "build muscle", "اكسب وزن", "ازيد وزن", "اضخم", "عضلات"))
                return "Weight Gain / Muscle Gain";

            if (ContainsAny(userMessage, "lose weight", "weight loss", "loss weight", "fat loss", "cut", "cutting", "اخس", "انحف", "تنشيف", "نزل وزن"))
                return "Weight Loss";

            if (ContainsAny(userMessage, "endurance", "fitness", "fit", "لياقة"))
                return "General Fitness";

            return null;
        }

        private static string BuildGoalAwareUserMessage(string userMessage, string effectiveGoal) =>
            $"Current requested goal, if different from profile, is: {effectiveGoal}.\nUser message: {userMessage}";

        private static string GetMemberName(MemberProfile profile)
        {
            if (!string.IsNullOrWhiteSpace(profile.FirstName))
                return profile.FirstName;

            if (!string.IsNullOrWhiteSpace(profile.User?.FirstName))
                return profile.User.FirstName;

            return "Member";
        }
        private static bool ContainsAny(string? text, params string[] values)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            return values.Any(value => text.Contains(value, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsKneeStressExercise(string? exerciseName) =>
            ContainsAny(
                exerciseName,
                "squat",
                "leg press",
                "lunge",
                "running",
                "run",
                "jump",
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
    }
}