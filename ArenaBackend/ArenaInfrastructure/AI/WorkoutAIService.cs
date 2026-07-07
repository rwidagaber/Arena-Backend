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
        public string? NameAr { get; set; }
        public int DurationWeeks { get; set; }
        public List<WorkoutDayAIResponse> Days { get; set; } = [];
    }

    public class WorkoutDayAIResponse
    {
        public string DayName { get; set; } = string.Empty;
        public string? DayNameAr { get; set; }
        public List<WorkoutExerciseAIResponse> Exercises { get; set; } = [];
    }

    public class WorkoutExerciseAIResponse
    {
        public string Name { get; set; } = string.Empty;
        public string? NameAr { get; set; }
        public string? Description { get; set; }
        public string? DescriptionAr { get; set; }
        public int Sets { get; set; }
        public int Reps { get; set; }
        public string MuscleGroup { get; set; } = string.Empty;
        public string? MuscleGroupAr { get; set; }
        public string? Equipment { get; set; }
        public string? EquipmentAr { get; set; }
        public List<string>? PrimaryMuscles { get; set; }
        public List<string>? PrimaryMusclesAr { get; set; }
        public List<string>? SecondaryMuscles { get; set; }
        public List<string>? SecondaryMusclesAr { get; set; }
        public List<string>? Instructions { get; set; }
        public List<string>? InstructionsAr { get; set; }
        public List<string>? CommonMistakes { get; set; }
        public List<string>? CommonMistakesAr { get; set; }
        public List<string>? SafetyTips { get; set; }
        public List<string>? SafetyTipsAr { get; set; }
        public string? Breathing { get; set; }
        public string? BreathingAr { get; set; }
        public string? Difficulty { get; set; }
        public string? DifficultyAr { get; set; }
        public string? Category { get; set; }
        public string? CategoryAr { get; set; }
        public string? VideoUrl { get; set; }
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

            var effectiveGoal = DetermineGoal(userMessage, profile.Goal);
            if (string.IsNullOrEmpty(effectiveGoal))
            {
                throw new GoalRequiredException("GOAL_REQUIRED");
            }

            var goalFromMessage = ExtractGoalFromMessage(userMessage);
            if (goalFromMessage != null && !string.Equals(profile.Goal, goalFromMessage, StringComparison.OrdinalIgnoreCase))
            {
                profile.Goal = goalFromMessage;
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
            await ResolveDuplicateExercisesAsync(planData, validCatalogItems);
            LocalizeWorkoutPlan(planData, WorkoutLocalization.IsArabic(userMessage), effectiveGoal);

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
                            NameAr = ex.NameAr,
                            MuscleGroup = ex.MuscleGroup,
                            MuscleGroupAr = ex.MuscleGroupAr,
                            Description = !string.IsNullOrEmpty(ex.Description) ? ex.Description : ex.Name,
                            DescriptionAr = ex.DescriptionAr,
                            Equipment = !string.IsNullOrEmpty(ex.Equipment) ? ex.Equipment : "None",
                            EquipmentAr = ex.EquipmentAr,
                            MemberProfileId = profile.Id
                        };
                        _context.Exercises.Add(existingExercise);
                    }

                    PopulateExerciseMetadata(existingExercise, ex);

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
                        ExerciseId = existingExercise.Id,
                        Sets = ex.Sets,
                        Reps = ex.Reps,
                        Exercise = MapToExerciseDto(existingExercise)
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

        public async Task<WorkoutPlanDto> ModifyWorkoutPlanAsync(Guid memberProfileId, string userMessage)
        {
            var profile = await _context.MemberProfiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == memberProfileId || p.UserId == memberProfileId);

            if (profile == null)
                throw new Exception($"Profile not found: {memberProfileId}");

            var activePlan = await _context.WorkoutPlans
                .Include(p => p.WorkoutDays)
                    .ThenInclude(d => d.Exercises)
                        .ThenInclude(e => e.Exercise)
                .Where(p => p.MemberProfileId == profile.Id && p.IsActive && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            if (activePlan == null)
            {
                return await GenerateWorkoutPlanAsync(memberProfileId, userMessage);
            }

            var currentPlanData = new
            {
                name = activePlan.Name,
                durationWeeks = activePlan.DurationWeeks,
                days = activePlan.WorkoutDays.Select(d => new
                {
                    dayName = d.DayName,
                    exercises = d.Exercises.Select(ex => new
                    {
                        name = !string.IsNullOrWhiteSpace(ex.ExrciseName) ? ex.ExrciseName : ex.Exercise?.Name ?? "Exercise",
                        sets = ex.Sets,
                        reps = ex.Reps
                    }).ToList()
                }).ToList()
            };

            var currentPlanJson = JsonSerializer.Serialize(currentPlanData, new JsonSerializerOptions { WriteIndented = true });

            var effectiveGoal = DetermineGoal(userMessage, profile.Goal);
            if (string.IsNullOrEmpty(effectiveGoal))
            {
                throw new GoalRequiredException("GOAL_REQUIRED");
            }

            var goalFromMessage = ExtractGoalFromMessage(userMessage);
            if (goalFromMessage != null && !string.Equals(profile.Goal, goalFromMessage, StringComparison.OrdinalIgnoreCase))
            {
                profile.Goal = goalFromMessage;
                _context.MemberProfiles.Update(profile);
            }
            var memberName = GetMemberName(profile);
            var goalAwareUserMessage = BuildGoalAwareUserMessage(userMessage, effectiveGoal);
            var healthContext = await _healthRAG.GetRelevantHealthContextAsync(profile.Id, goalAwareUserMessage);

            HealthProfileDto healthProfile = new HealthProfileDto();
            if (!string.IsNullOrWhiteSpace(profile.HealthProfileJson))
            {
                healthProfile = JsonSerializer.Deserialize<HealthProfileDto>(profile.HealthProfileJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new HealthProfileDto();
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

            var prompt = $"""
            You are an expert personal trainer with 20 years of experience.
            
            The user has an ACTIVE workout plan:
            {currentPlanJson}
            
            === USER REQUEST ===
            The user wants to modify their workout plan with the following request:
            "{userMessage}"

            === MEMBER'S KNOWN HEALTH HISTORY (CRITICAL - MUST RESPECT) ===
            {healthContext}

            === STRICT MEDICAL GUIDELINES ===
            {await _healthIntelligence.RetrieveMedicalGuidelinesAsync(healthProfile)}
            
            === INSTRUCTIONS ===
            1. Apply the user's modification request to the plan.
            2. Preserve as much of the existing exercises, sets, reps, and structure as possible. Only make changes necessary to satisfy the request (e.g. replace exercises, adjust workout days/volume, etc.).
            3. Respect the user's injuries and health conditions.
            4. Completely exclude any exercises or movements they request to avoid/replace.
            5. Return the updated plan in the EXACT same JSON format.
            6. Return ONLY the valid JSON response. No extra text, no markdown.
            """;

            WorkoutPlanAIResponse planData = null;
            int retries = 0;
            bool isValid = false;
            string currentPrompt = prompt;

            while (retries < 3 && !isValid)
            {
                try
                {
                    var jsonResponse = await _gemini.GetCompletionAsync(currentPrompt, new List<ChatMessageDto>(), "Modify the plan");
                    var cleanJson = AIHelper.CleanJson(jsonResponse);
                    planData = JsonSerializer.Deserialize<WorkoutPlanAIResponse>(
                        cleanJson,
                        CreateJsonOptions());

                    var validationResult = await _healthIntelligence.ValidatePlanAsync(healthProfile, cleanJson, "Workout");
                    
                    if (validationResult.IsValid)
                    {
                        isValid = true;
                    }
                    else
                    {
                        retries++;
                        currentPrompt = prompt + $"\n\n[CRITICAL FEEDBACK - REGENERATION REQUIRED]: Your modified plan was REJECTED by the Medical Validation Layer for the following reason:\n{validationResult.RejectionReason}\nYou MUST fix this immediately and provide a safe plan.";
                    }
                }
                catch
                {
                    retries++;
                }
            }

            if (!isValid || planData == null)
            {
                return await GenerateWorkoutPlanAsync(memberProfileId, userMessage);
            }

            NormalizeWorkoutPlan(planData, profile, memberName, goalAwareUserMessage, healthContext, effectiveGoal);
            ApplyEquipmentSubstitution(planData, catalogItems, validCatalogItems);
            await ResolveDuplicateExercisesAsync(planData, validCatalogItems);
            LocalizeWorkoutPlan(planData, WorkoutLocalization.IsArabic(userMessage), effectiveGoal);

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
                            NameAr = ex.NameAr,
                            MuscleGroup = ex.MuscleGroup,
                            MuscleGroupAr = ex.MuscleGroupAr,
                            Description = !string.IsNullOrEmpty(ex.Description) ? ex.Description : ex.Name,
                            DescriptionAr = ex.DescriptionAr,
                            Equipment = !string.IsNullOrEmpty(ex.Equipment) ? ex.Equipment : "None",
                            EquipmentAr = ex.EquipmentAr,
                            MemberProfileId = profile.Id
                        };
                        _context.Exercises.Add(existingExercise);
                    }

                    PopulateExerciseMetadata(existingExercise, ex);

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
                        Reps = ex.Reps,
                        ExerciseId = existingExercise.Id,
                        Exercise = MapToExerciseDto(existingExercise)
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

            var savedPlan = await _context.WorkoutPlans.FirstOrDefaultAsync(p => p.Id == plan.Id);
            if (savedPlan == null)
            {
                throw new Exception("Persistence verification failed: workout plan was not correctly saved.");
            }

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
                planData.Name = WorkoutLocalization.GetLocalizedPlanName(effectiveGoal, WorkoutLocalization.IsArabic(userMessage));

            if (planData.DurationWeeks <= 0)
                planData.DurationWeeks = 4;

            planData.Days ??= [];
            var avoidKneeStress = ContainsAny(profile.Injuries, "knee", "ركبة")
                || ContainsAny(userMessage, "knee", "ركبة")
                || ContainsAny(healthContext, "knee", "ركبة")
                || ContainsAny(healthContext, "acl", "meniscus");

            var avoidShoulderStress = ContainsAny(profile.Injuries, "shoulder", "rotator", "arm", "كتف", "ذراع")
                || ContainsAny(userMessage, "shoulder", "rotator", "arm", "كتف", "ذراع")
                || ContainsAny(healthContext, "shoulder", "rotator", "arm", "كتف", "ذراع");

            var avoidBackStress = ContainsAny(profile.Injuries, "back", "spine", "lumber", "ظهر")
                || ContainsAny(userMessage, "back", "spine", "lumber", "ظهر")
                || ContainsAny(healthContext, "back", "spine", "lumber", "ظهر");

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

                    if (avoidShoulderStress && IsShoulderStressExercise(exercise.Name))
                        ReplaceWithShoulderFriendlyExercise(exercise);

                    if (avoidBackStress && IsBackStressExercise(exercise.Name))
                        ReplaceWithBackFriendlyExercise(exercise);
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

        private async Task ResolveDuplicateExercisesAsync(WorkoutPlanAIResponse planData, List<ExerciseCatalogItem> validCatalog)
        {
            if (planData.Days == null) return;

            foreach (var day in planData.Days)
            {
                if (day.Exercises == null) continue;

                var seenExerciseNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var ex in day.Exercises)
                {
                    if (string.IsNullOrWhiteSpace(ex.Name)) continue;

                    if (seenExerciseNames.Contains(ex.Name))
                    {
                        // Duplicate detected! Try to find a replacement targeting the same muscle group
                        var targetMuscleGroup = ex.MuscleGroup;
                        if (string.IsNullOrEmpty(targetMuscleGroup))
                        {
                            var matched = validCatalog.FirstOrDefault(c => c.Name.Equals(ex.Name, StringComparison.OrdinalIgnoreCase));
                            if (matched != null) targetMuscleGroup = matched.MuscleGroup;
                        }
                        if (string.IsNullOrEmpty(targetMuscleGroup)) targetMuscleGroup = "General";

                        // Find candidates from validCatalog that target the same muscle group and are NOT in the seen list
                        var replacementItem = validCatalog
                            .FirstOrDefault(c => c.MuscleGroup.Equals(targetMuscleGroup, StringComparison.OrdinalIgnoreCase)
                                && !seenExerciseNames.Contains(c.Name));

                        if (replacementItem == null)
                        {
                            // Try to find ANY exercise in the catalog that is not yet used on this day
                            replacementItem = validCatalog
                                .FirstOrDefault(c => !seenExerciseNames.Contains(c.Name));
                        }

                        if (replacementItem != null)
                        {
                            var replacementName = replacementItem.Name;

                            // Look for an existing template in the database to get high-quality instructions/tips
                            var template = await _context.Exercises
                                .FirstOrDefaultAsync(e => e.Name == replacementName && e.Instructions != null);

                            ex.Name = replacementItem.Name;
                            ex.NameAr = replacementItem.NameAr;
                            ex.MuscleGroup = replacementItem.MuscleGroup;
                            ex.MuscleGroupAr = replacementItem.MuscleGroupAr;

                            if (template != null)
                            {
                                ex.Description = template.Description;
                                ex.DescriptionAr = template.DescriptionAr;
                                ex.Instructions = SafeDeserializeList(template.Instructions);
                                ex.InstructionsAr = SafeDeserializeList(template.InstructionsAr);
                                ex.CommonMistakes = SafeDeserializeList(template.CommonMistakes);
                                ex.CommonMistakesAr = SafeDeserializeList(template.CommonMistakesAr);
                                ex.SafetyTips = SafeDeserializeList(template.SafetyTips);
                                ex.SafetyTipsAr = SafeDeserializeList(template.SafetyTipsAr);
                                ex.Breathing = template.Breathing;
                                ex.BreathingAr = template.BreathingAr;
                                ex.Difficulty = template.Difficulty;
                                ex.DifficultyAr = template.DifficultyAr;
                                ex.Category = template.Category;
                                ex.CategoryAr = template.CategoryAr;
                                ex.VideoUrl = template.VideoUrl;
                                ex.Equipment = template.Equipment;
                                ex.EquipmentAr = template.EquipmentAr;
                                ex.PrimaryMuscles = SafeDeserializeList(template.PrimaryMuscles);
                                ex.PrimaryMusclesAr = SafeDeserializeList(template.PrimaryMusclesAr);
                                ex.SecondaryMuscles = SafeDeserializeList(template.SecondaryMuscles);
                                ex.SecondaryMusclesAr = SafeDeserializeList(template.SecondaryMusclesAr);
                            }
                            else
                            {
                                ex.Description = replacementItem.Description;
                                ex.DescriptionAr = replacementItem.DescriptionAr;
                                ex.Instructions = new List<string> { "Start in a stable position.", "Execute the exercise with control.", "Perform the reps under control and return to start position." };
                                ex.InstructionsAr = new List<string> { "ابدأ في وضع ثابت.", "نفذ التمرين بتحكم.", "قم بأداء التكرارات بتحكم وعد إلى وضع البداية." };
                                ex.Breathing = "Inhale during eccentric phase, exhale during concentric phase.";
                                ex.BreathingAr = "الشهيق أثناء الحركة السلبية، والزفير أثناء الحركة الإيجابية.";
                                ex.CommonMistakes = new List<string> { "Using momentum to lift.", "Improper body alignment." };
                                ex.CommonMistakesAr = new List<string> { "استخدام قوة الدفع للرفع.", "محاذاة غير صحيحة للجسم." };
                                ex.SafetyTips = new List<string> { "Keep your core engaged.", "Do not lock out your joints." };
                                ex.SafetyTipsAr = new List<string> { "حافظ على تفعيل عضلات الجذع.", "لا تقفل مفاصلك بالكامل." };
                                ex.Difficulty = replacementItem.DifficultyLevel;
                                ex.DifficultyAr = replacementItem.DifficultyLevel == "Beginner" ? "مبتدئ" : replacementItem.DifficultyLevel == "Intermediate" ? "متوسط" : "متقدم";
                                ex.Category = "Strength";
                                ex.CategoryAr = "قوة";
                                ex.Equipment = "Gym Equipment";
                                ex.EquipmentAr = "معدات الصالة الرياضية";
                                ex.PrimaryMuscles = new List<string> { replacementItem.MuscleGroup };
                                ex.PrimaryMusclesAr = new List<string> { replacementItem.MuscleGroupAr ?? replacementItem.MuscleGroup };
                                ex.SecondaryMuscles = new List<string>();
                                ex.SecondaryMusclesAr = new List<string>();
                            }
                        }
                    }

                    seenExerciseNames.Add(ex.Name);
                }
            }
        }

        private static List<string>? SafeDeserializeList(string? json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                var trimmed = json.Trim();
                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    return JsonSerializer.Deserialize<List<string>>(json);
                }
                return new List<string> { json };
            }
            catch
            {
                return new List<string> { json };
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
                Name = WorkoutLocalization.GetLocalizedPlanName(effectiveGoal, WorkoutLocalization.IsArabic(userMessage)),
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

        private static string? DetermineGoal(string? userMessage, string? dbGoal)
        {
            var extracted = ExtractGoalFromMessage(userMessage);
            if (extracted != null)
                return extracted;

            if (!string.IsNullOrWhiteSpace(dbGoal))
            {
                var normalized = NormalizeGoalName(dbGoal);
                if (normalized != null)
                    return normalized;
            }

            return null;
        }

        private static string? ExtractGoalFromMessage(string? userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage)) return null;

            if (ContainsAny(userMessage, 
                "lose weight", "weight loss", "loss weight", "burn fat", "fat burn", "get lean", "lean out", "cutting", "cut",
                "أخس", "اخس", "أنزل وزن", "انزل وزن", "أحرق دهون", "احرق دهون", "أتخلص من الكرش", "اتخلص من الكرش", "تنشيف", "نحافة", "انحف", "أنحف"))
            {
                return "Weight Loss";
            }

            if (ContainsAny(userMessage, 
                "gain weight", "weight gain", "increase weight", "bulk", "bulking", "gain muscle", "muscle gain", "build muscle",
                "أتخن", "اتخن", "أزيد وزن", "ازيد وزن", "أبني عضلات", "ابني عضلات", "أضخم", "اضخم", "أزود كتلة عضلية", "ازود كتلة عضلية", "تضخيم"))
            {
                return "Muscle Gain";
            }

            if (ContainsAny(userMessage, "stronger chest", "bigger chest", "build chest", "develop chest", "أقوي صدري", "اقوي صدري", "أكبر صدري", "اكبر صدري", "تضخيم الصدر", "تمرين صدر"))
            {
                return "Chest Hypertrophy";
            }

            if (ContainsAny(userMessage, "stronger legs", "bigger legs", "build legs", "leg strength", "leg hypertrophy", "أقوي رجلي", "اقوي رجلي", "أكبر رجلي", "اكبر رجلي", "تضخيم الرجل"))
            {
                return "Leg Hypertrophy";
            }

            if (ContainsAny(userMessage, "stronger back", "bigger back", "build back", "back strength", "back hypertrophy", "أقوي ضهري", "اقوي ضهري", "أكبر ضهري", "اكبر ضهري", "تضخيم الظهر"))
            {
                return "Back Strength";
            }

            if (ContainsAny(userMessage, "stronger shoulders", "bigger shoulders", "build shoulders", "shoulder strength", "shoulder hypertrophy", "أقوي كتفي", "اقوي كتفي", "أكبر كتفي", "اكبر كتفي", "تضخيم الكتف"))
            {
                return "Shoulder Hypertrophy";
            }

            if (ContainsAny(userMessage, "stronger arms", "bigger arms", "build arms", "arm strength", "arm hypertrophy", "أقوي دراعاتي", "اقوي دراعاتي", "أكبر دراعاتي", "اكبر دراعاتي", "أكبر دراع", "اكبر دراع", "تضخيم الذراع"))
            {
                return "Arm Hypertrophy";
            }

            if (ContainsAny(userMessage, "glutes", "bigger glutes", "glute training", "أكبر مؤخرة", "اكبر مؤخرة", "تضخيم المؤخرة", "الأرداف"))
            {
                return "Glutes Hypertrophy";
            }

            if (ContainsAny(userMessage, "core", "abs", "six pack", "أقوي بطني", "اقوي بطني", "عضلات بطن"))
            {
                return "Core Strength";
            }

            if (ContainsAny(userMessage, "improve my fitness", "improve fitness", "general fitness", "fitness level", "أقوي اللياقة", "اقوي اللياقة", "تحسين اللياقة", "لياقة"))
            {
                return "General Fitness";
            }

            if (ContainsAny(userMessage, "increase strength", "improve strength", "get stronger", "my strength", "أزود قوتي", "ازود قوتي", "زيادة القوة", "قوة"))
            {
                return "Strength";
            }

            return null;
        }

        private static string? NormalizeGoalName(string dbGoal)
        {
            if (string.IsNullOrWhiteSpace(dbGoal)) return null;

            if (ContainsAny(dbGoal, "loss", "lose", "cut", "تخسيس", "خسارة", "تنشيف"))
                return "Weight Loss";

            if (ContainsAny(dbGoal, "gain", "bulk", "تضخيم", "بناء"))
                return "Muscle Gain";

            if (ContainsAny(dbGoal, "chest"))
                return "Chest Hypertrophy";

            if (ContainsAny(dbGoal, "leg"))
                return "Leg Hypertrophy";

            if (ContainsAny(dbGoal, "back"))
                return "Back Strength";

            if (ContainsAny(dbGoal, "shoulder"))
                return "Shoulder Hypertrophy";

            if (ContainsAny(dbGoal, "arm"))
                return "Arm Hypertrophy";

            if (ContainsAny(dbGoal, "glute"))
                return "Glutes Hypertrophy";

            if (ContainsAny(dbGoal, "core", "abs"))
                return "Core Strength";

            if (ContainsAny(dbGoal, "endurance", "fitness", "fit", "لياقة"))
                return "General Fitness";

            if (ContainsAny(dbGoal, "strength", "قوة"))
                return "Strength";

            return dbGoal;
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

        private static bool IsShoulderStressExercise(string? name) =>
            ContainsAny(name, "shoulder press", "overhead press", "military press", "overhead", "ضغط كتف");

        private static void ReplaceWithShoulderFriendlyExercise(WorkoutExerciseAIResponse ex)
        {
            ex.Name = "Cable Face Pull";
            ex.MuscleGroup = "Shoulders";
            ex.Sets = ex.Sets <= 0 ? 3 : ex.Sets;
            ex.Reps = ex.Reps <= 0 ? 15 : ex.Reps;
        }

        private static bool IsBackStressExercise(string? name) =>
            ContainsAny(name, "deadlift", "barbell squat", "heavy row", "t-bar row", "dead lift", "رفعة مميتة");

        private static void ReplaceWithBackFriendlyExercise(WorkoutExerciseAIResponse ex)
        {
            ex.Name = "Hyperextension";
            ex.MuscleGroup = "Lower Back";
            ex.Sets = ex.Sets <= 0 ? 3 : ex.Sets;
            ex.Reps = ex.Reps <= 0 ? 12 : ex.Reps;
        }

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

        private static void LocalizeWorkoutPlan(WorkoutPlanAIResponse planData, bool isArabic, string effectiveGoal)
        {
            planData.Name = WorkoutLocalization.GetLocalizedPlanName(effectiveGoal, isArabic);

            if (isArabic)
            {
                foreach (var day in planData.Days ?? [])
                {
                    day.DayName = WorkoutLocalization.TranslateDay(day.DayName);
                    foreach (var ex in day.Exercises ?? [])
                    {
                        var englishName = ex.Name;
                        ex.Name = WorkoutLocalization.TranslateExercise(englishName);
                        ex.NameAr = ex.Name;
                        ex.MuscleGroupAr = ex.MuscleGroup;
                    }
                }
            }
        }

        private static void PopulateExerciseMetadata(Exercise entity, WorkoutExerciseAIResponse response)
        {
            if (response.PrimaryMuscles != null && response.PrimaryMuscles.Count > 0)
                entity.PrimaryMuscles = JsonSerializer.Serialize(response.PrimaryMuscles);
            if (response.PrimaryMusclesAr != null && response.PrimaryMusclesAr.Count > 0)
                entity.PrimaryMusclesAr = JsonSerializer.Serialize(response.PrimaryMusclesAr);
            if (response.SecondaryMuscles != null && response.SecondaryMuscles.Count > 0)
                entity.SecondaryMuscles = JsonSerializer.Serialize(response.SecondaryMuscles);
            if (response.SecondaryMusclesAr != null && response.SecondaryMusclesAr.Count > 0)
                entity.SecondaryMusclesAr = JsonSerializer.Serialize(response.SecondaryMusclesAr);
            if (response.Instructions != null && response.Instructions.Count > 0)
                entity.Instructions = JsonSerializer.Serialize(response.Instructions);
            if (response.InstructionsAr != null && response.InstructionsAr.Count > 0)
                entity.InstructionsAr = JsonSerializer.Serialize(response.InstructionsAr);
            if (response.CommonMistakes != null && response.CommonMistakes.Count > 0)
                entity.CommonMistakes = JsonSerializer.Serialize(response.CommonMistakes);
            if (response.CommonMistakesAr != null && response.CommonMistakesAr.Count > 0)
                entity.CommonMistakesAr = JsonSerializer.Serialize(response.CommonMistakesAr);
            if (response.SafetyTips != null && response.SafetyTips.Count > 0)
                entity.SafetyTips = JsonSerializer.Serialize(response.SafetyTips);
            if (response.SafetyTipsAr != null && response.SafetyTipsAr.Count > 0)
                entity.SafetyTipsAr = JsonSerializer.Serialize(response.SafetyTipsAr);

            if (!string.IsNullOrEmpty(response.Description))
                entity.Description = response.Description;
            if (!string.IsNullOrEmpty(response.DescriptionAr))
                entity.DescriptionAr = response.DescriptionAr;
            if (!string.IsNullOrEmpty(response.Breathing))
                entity.Breathing = response.Breathing;
            if (!string.IsNullOrEmpty(response.BreathingAr))
                entity.BreathingAr = response.BreathingAr;
            if (!string.IsNullOrEmpty(response.Difficulty))
                entity.Difficulty = response.Difficulty;
            if (!string.IsNullOrEmpty(response.DifficultyAr))
                entity.DifficultyAr = response.DifficultyAr;
            if (!string.IsNullOrEmpty(response.Category))
                entity.Category = response.Category;
            if (!string.IsNullOrEmpty(response.CategoryAr))
                entity.CategoryAr = response.CategoryAr;
            if (!string.IsNullOrEmpty(response.VideoUrl))
                entity.VideoUrl = response.VideoUrl;
            if (!string.IsNullOrEmpty(response.Equipment))
                entity.Equipment = response.Equipment;
            if (!string.IsNullOrEmpty(response.EquipmentAr))
                entity.EquipmentAr = response.EquipmentAr;
        }

        private static ExerciseDto MapToExerciseDto(Exercise entity)
        {
            return new ExerciseDto
            {
                Id = entity.Id,
                Name = entity.Name,
                NameAr = entity.NameAr,
                Description = entity.Description,
                DescriptionAr = entity.DescriptionAr,
                MuscleGroup = entity.MuscleGroup,
                MuscleGroupAr = entity.MuscleGroupAr,
                Equipment = entity.Equipment,
                EquipmentAr = entity.EquipmentAr,
                VideoUrl = entity.VideoUrl,
                ImageUrl = entity.ImageUrl,
                PrimaryMuscles = entity.PrimaryMuscles,
                PrimaryMusclesAr = entity.PrimaryMusclesAr,
                SecondaryMuscles = entity.SecondaryMuscles,
                SecondaryMusclesAr = entity.SecondaryMusclesAr,
                Instructions = entity.Instructions,
                InstructionsAr = entity.InstructionsAr,
                CommonMistakes = entity.CommonMistakes,
                CommonMistakesAr = entity.CommonMistakesAr,
                SafetyTips = entity.SafetyTips,
                SafetyTipsAr = entity.SafetyTipsAr,
                Breathing = entity.Breathing,
                BreathingAr = entity.BreathingAr,
                Difficulty = entity.Difficulty,
                DifficultyAr = entity.DifficultyAr,
                Category = entity.Category,
                CategoryAr = entity.CategoryAr,
                MemberProfileId = entity.MemberProfileId
            };
        }
    }
}