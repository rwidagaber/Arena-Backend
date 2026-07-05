using ArenaApplication.AI;
using ArenaApplication.Dtos.ChatDtos;
using ArenaApplication.Dtos.Nutrition;
using ArenaApplication.Dtos.HealthIntelligence;
using ArenaApplication.IServices;
using ArenaDomain.Entities.Nutrition;
using ArenaDomain.Enums;
using ArenaInfrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ArenaInfrastructure.AI
{
    public class NutritionPlanAIResponse
    {
        public decimal DailyCalories { get; set; }
        public decimal ProteinGrams { get; set; }
        public decimal CarbsGrams { get; set; }
        public decimal FatGrams { get; set; }
        public List<MealAIResponse> Meals { get; set; } = [];
    }

    public class MealAIResponse
    {
        public string MealType { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Calories { get; set; }
        public decimal ProteinGrams { get; set; }
        public decimal CarbsGrams { get; set; }
        public decimal FatGrams { get; set; }
        public string Ingredients { get; set; } = string.Empty;
    }

    public class NutritionAIService : INutritionAIService
    {
        private readonly IGeminiCompletionService _gemini;
        private readonly AppDbContext _context;
        private readonly IMemberHealthRAGService _healthRAG;
        private readonly INotificationService _notificationService; // ✅
        private readonly IHealthIntelligenceService _healthIntelligence;

        public NutritionAIService(
            IGeminiCompletionService gemini,
            AppDbContext context,
            IMemberHealthRAGService healthRAG,
            INotificationService notificationService,
            IHealthIntelligenceService healthIntelligence) // ✅
        {
            _gemini = gemini;
            _context = context;
            _healthRAG = healthRAG;
            _notificationService = notificationService; // ✅
            _healthIntelligence = healthIntelligence;
        }

        public async Task<NutritionPlanResponseDto> GenerateNutritionPlanAsync(
            Guid memberProfileId, string userMessage)
        {
            var profile = await _context.MemberProfiles
                .FirstOrDefaultAsync(p => p.Id == memberProfileId
                                       || p.UserId == memberProfileId);

            if (profile == null)
                throw new Exception($"MemberProfile not found for Id: {memberProfileId}");

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

            //  Build subscription for context
            var subscription = await _context.UserSubscriptions
                .FirstOrDefaultAsync(s => s.MemberProfileId == profile.Id
                                       && s.Status == SubscriptionStatus.Active
                                       && s.EndDate > DateTime.UtcNow);

            //  Build userContext
            var userContext = UserContextBuilder.Build(
                profile,
                subscription,
                recentProgress: recentProgress,
                nutritionPlans: recentNutritionPlans,
                workoutPlans: recentWorkoutPlans);

            HealthProfileDto healthProfile = new HealthProfileDto();
            if (!string.IsNullOrWhiteSpace(profile.HealthProfileJson))
            {
                healthProfile = System.Text.Json.JsonSerializer.Deserialize<HealthProfileDto>(profile.HealthProfileJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new HealthProfileDto();
            }

            var medicalGuidelines = await _healthIntelligence.RetrieveMedicalGuidelinesAsync(healthProfile);
            if (!string.IsNullOrWhiteSpace(medicalGuidelines))
            {
                healthContext += $"\n\n=== STRICT MEDICAL GUIDELINES (WHO/CDC/NHS) ===\n{medicalGuidelines}";
            }

            var prompt = PromptLoader.GetNutritionPrompt(
                userContext: userContext,
                goal: effectiveGoal,
                dietaryRestrictions: profile.DietaryRestrictions ?? "None",
                healthConditions: profile.HealthConditions ?? "None",
                userMessage: BuildHealthAwareUserMessage(goalAwareUserMessage, healthContext));

            NutritionPlanAIResponse planData = null;
            int retries = 0;
            bool isValid = false;
            string currentPrompt = prompt;

            while (retries < 3 && !isValid)
            {
                try
                {
                    var jsonResponse = await _gemini.GetCompletionAsync(
                        currentPrompt, new List<ChatMessageDto>(), "Generate the plan");

                    var cleanJson = AIHelper.CleanJson(jsonResponse);
                    planData = JsonSerializer.Deserialize<NutritionPlanAIResponse>(
                        cleanJson,
                        CreateJsonOptions()) ?? CreateFallbackPlanData(profile, effectiveGoal);

                    var validationResult = await _healthIntelligence.ValidatePlanAsync(healthProfile, cleanJson, "Nutrition");
                    
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
                planData = CreateFallbackPlanData(profile, effectiveGoal);
            }

            ApplyLocalNutritionReplacements(planData, healthContext);
            LocalizePlanData(planData, WorkoutLocalization.IsArabic(userMessage));

            var activeNutritionPlans = await _context.NutritionPlans
                .Where(existingPlan => existingPlan.MemberProfileId == profile.Id
                    && existingPlan.IsActive
                    && !existingPlan.IsDeleted)
                .ToListAsync();

            foreach (var existingPlan in activeNutritionPlans)
            {
                existingPlan.IsActive = false;
                existingPlan.UpdatedAt = DateTime.UtcNow;
            }

            var plan = new NutritionPlan
            {
                MemberProfileId = profile.Id,
                DailyCalories = planData.DailyCalories,
                ProteinGrams = planData.ProteinGrams,
                CarbsGrams = planData.CarbsGrams,
                FatGrams = planData.FatGrams,
                IsActive = true
            };

            _context.NutritionPlans.Add(plan);
            await _context.SaveChangesAsync();

            var mealDtos = new List<MealResponseDto>();

            foreach (var meal in planData.Meals ?? [])
            {
                var mealEntity = new Meal
                {
                    NutritionPlanId = plan.Id,
                    MealType = meal.MealType,
                    Name = meal.Name,
                    Calories = meal.Calories,
                    Ingredients = meal.Ingredients,
                    Protein = meal.ProteinGrams,
                    Carbs = meal.CarbsGrams,
                    Fat = meal.FatGrams
                };

                _context.Meals.Add(mealEntity);

                mealDtos.Add(new MealResponseDto
                {
                    MealType = meal.MealType,
                    Name = meal.Name,
                    Calories = meal.Calories,
                    ProteinGrams = meal.ProteinGrams,
                    CarbsGrams = meal.CarbsGrams,
                    FatGrams = meal.FatGrams,
                    Ingredients = meal.Ingredients
                });
            }

            await _context.SaveChangesAsync();

            await _notificationService.NotifyNutritionPlanReadyAsync(profile.Id);

            return new NutritionPlanResponseDto
            {
                Id = plan.Id,
                DailyCalories = plan.DailyCalories,
                ProteinGrams = plan.ProteinGrams,
                CarbsGrams = plan.CarbsGrams,
                FatGrams = plan.FatGrams,
                IsActive = plan.IsActive,
                Meals = mealDtos
            };
        }

        public async Task<NutritionPlanResponseDto> ModifyNutritionPlanAsync(
            Guid memberProfileId, string userMessage)
        {
            var profile = await _context.MemberProfiles
                .FirstOrDefaultAsync(p => p.Id == memberProfileId || p.UserId == memberProfileId);

            if (profile == null)
                throw new Exception($"MemberProfile not found for Id: {memberProfileId}");

            var activePlan = await _context.NutritionPlans
                .Include(p => p.Meals)
                .Where(p => p.MemberProfileId == profile.Id && p.IsActive && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            if (activePlan == null)
            {
                return await GenerateNutritionPlanAsync(memberProfileId, userMessage);
            }

            var currentPlanData = new
            {
                dailyCalories = activePlan.DailyCalories,
                proteinGrams = activePlan.ProteinGrams,
                carbsGrams = activePlan.CarbsGrams,
                fatGrams = activePlan.FatGrams,
                meals = activePlan.Meals.Select(m => new
                {
                    mealType = m.MealType,
                    name = m.Name,
                    calories = m.Calories,
                    proteinGrams = m.Protein,
                    carbsGrams = m.Carbs,
                    fatGrams = m.Fat,
                    ingredients = m.Ingredients
                }).ToList()
            };

            var currentPlanJson = JsonSerializer.Serialize(currentPlanData, new JsonSerializerOptions { WriteIndented = true });
            HealthProfileDto healthProfile = new HealthProfileDto();
            if (!string.IsNullOrWhiteSpace(profile.HealthProfileJson))
            {
                healthProfile = JsonSerializer.Deserialize<HealthProfileDto>(profile.HealthProfileJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new HealthProfileDto();
            }

            var healthContext = await _healthRAG.GetRelevantHealthContextAsync(profile.Id, userMessage);
            var medicalGuidelines = await _healthIntelligence.RetrieveMedicalGuidelinesAsync(healthProfile);
 
            var prompt = $"""
            You are a certified nutritionist and dietitian.
            
            The user has an ACTIVE nutrition plan:
            {currentPlanJson}
            
            === USER REQUEST ===
            The user wants to modify their nutrition plan with the following request:
            "{userMessage}"

            === MEMBER'S KNOWN HEALTH HISTORY (CRITICAL - MUST RESPECT) ===
            {healthContext}

            === STRICT MEDICAL GUIDELINES ===
            {medicalGuidelines}
            
            === INSTRUCTIONS ===
            1. Apply the user's modification request to the plan.
            2. Preserve as much of the existing meals, calories, ingredients, and structures as possible. Only make changes necessary to satisfy the request (e.g. swap foods, adjust macros slightly if needed, remove ingredients, etc.).
            3. Respect the user's health profile and conditions.
            4. Completely exclude any foods or ingredients they request to avoid/replace.
            5. Return the updated plan in the EXACT same JSON format.
            6. Return ONLY the valid JSON response. No extra text, no markdown.
            """;

            NutritionPlanAIResponse planData = null;
            int retries = 0;
            bool isValid = false;
            string currentPrompt = prompt;

            while (retries < 3 && !isValid)
            {
                try
                {
                    var jsonResponse = await _gemini.GetCompletionAsync(
                        currentPrompt, new List<ChatMessageDto>(), "Modify the plan");

                    var cleanJson = AIHelper.CleanJson(jsonResponse);
                    planData = JsonSerializer.Deserialize<NutritionPlanAIResponse>(
                        cleanJson,
                        CreateJsonOptions());

                    var validationResult = await _healthIntelligence.ValidatePlanAsync(healthProfile, cleanJson, "Nutrition");
                    
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
                catch (Exception)
                {
                    retries++;
                }
            }

            if (!isValid || planData == null)
            {
                return await GenerateNutritionPlanAsync(memberProfileId, userMessage);
            }

            NormalizeNutritionPlan(planData);
            ApplyLocalNutritionReplacements(planData, healthContext);
            LocalizePlanData(planData, WorkoutLocalization.IsArabic(userMessage));

            var activeNutritionPlans = await _context.NutritionPlans
                .Where(existingPlan => existingPlan.MemberProfileId == profile.Id
                    && existingPlan.IsActive
                    && !existingPlan.IsDeleted)
                .ToListAsync();

            foreach (var existingPlan in activeNutritionPlans)
            {
                existingPlan.IsActive = false;
                existingPlan.UpdatedAt = DateTime.UtcNow;
            }

            var plan = new NutritionPlan
            {
                Id = Guid.NewGuid(),
                MemberProfileId = profile.Id,
                DailyCalories = planData.DailyCalories,
                ProteinGrams = planData.ProteinGrams,
                CarbsGrams = planData.CarbsGrams,
                FatGrams = planData.FatGrams,
                IsActive = true
            };

            _context.NutritionPlans.Add(plan);

            var mealDtos = new List<MealResponseDto>();

            foreach (var meal in planData.Meals ?? [])
            {
                var mealEntity = new Meal
                {
                    Id = Guid.NewGuid(),
                    NutritionPlanId = plan.Id,
                    MealType = meal.MealType,
                    Name = meal.Name,
                    Calories = meal.Calories,
                    Ingredients = meal.Ingredients,
                    Protein = meal.ProteinGrams,
                    Carbs = meal.CarbsGrams,
                    Fat = meal.FatGrams
                };

                _context.Meals.Add(mealEntity);

                mealDtos.Add(new MealResponseDto
                {
                    Id = mealEntity.Id,
                    MealType = meal.MealType,
                    Name = meal.Name,
                    Calories = meal.Calories,
                    ProteinGrams = meal.ProteinGrams,
                    CarbsGrams = meal.CarbsGrams,
                    FatGrams = meal.FatGrams,
                    Ingredients = meal.Ingredients
                });
            }

            await _context.SaveChangesAsync();

            var savedPlan = await _context.NutritionPlans.FirstOrDefaultAsync(p => p.Id == plan.Id);
            if (savedPlan == null)
            {
                throw new Exception("Persistence verification failed: nutrition plan was not correctly saved.");
            }

            await _notificationService.NotifyNutritionPlanReadyAsync(profile.Id);

            return new NutritionPlanResponseDto
            {
                Id = plan.Id,
                DailyCalories = plan.DailyCalories,
                ProteinGrams = plan.ProteinGrams,
                CarbsGrams = plan.CarbsGrams,
                FatGrams = plan.FatGrams,
                IsActive = plan.IsActive,
                Meals = mealDtos
            };
        }

        private static JsonSerializerOptions CreateJsonOptions()
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            options.Converters.Add(new FlexibleDecimalConverter());
            return options;
        }

        private static void NormalizeNutritionPlan(NutritionPlanAIResponse planData)
        {
            if (planData.DailyCalories <= 0)
                planData.DailyCalories = 2000;

            if (planData.ProteinGrams <= 0)
                planData.ProteinGrams = 120;

            if (planData.CarbsGrams <= 0)
                planData.CarbsGrams = 220;

            if (planData.FatGrams <= 0)
                planData.FatGrams = 65;

            planData.Meals ??= [];

            foreach (var meal in planData.Meals)
            {
                if (string.IsNullOrWhiteSpace(meal.MealType))
                    meal.MealType = "Meal";

                if (string.IsNullOrWhiteSpace(meal.Name))
                    meal.Name = "Balanced meal";

                if (meal.Calories <= 0)
                    meal.Calories = 400;

                if (meal.ProteinGrams <= 0)
                    meal.ProteinGrams = 25;

                if (meal.CarbsGrams <= 0)
                    meal.CarbsGrams = 40;

                if (meal.FatGrams <= 0)
                    meal.FatGrams = 12;

                if (string.IsNullOrWhiteSpace(meal.Ingredients))
                    meal.Ingredients = "Lean protein, complex carbohydrates, vegetables, healthy fats";
            }
        }

        private static NutritionPlanAIResponse CreateFallbackPlanData(ArenaDomain.Entities.MemberProfile profile, string effectiveGoal)
        {
            var weight = Math.Clamp(profile.Weight ?? 70m, 35m, 220m);
            var height = Math.Clamp(profile.Height ?? 170m, 120m, 230m);
            var age = CalculateAge(profile.DateOfBirth);
            var isFemale = profile.Gender == Gender.Female;
            var isWeightLoss = IsWeightLossGoal(profile);
            var isMuscleGain = ContainsAny(profile.Goal, "muscle", "gain", "bulk", "عضلات", "اكسب");
            var hasDiabetes = ContainsAny(profile.HealthConditions, "diabetes", "sugar", "سكري");
            var isVegetarian = ContainsAny(profile.DietaryRestrictions, "vegetarian", "vegan", "نباتي");

            var bmr = 10m * weight + 6.25m * height - 5m * age + (isFemale ? -161m : 5m);
            var maintenance = bmr * GetActivityMultiplier(profile.ActivityLevel);
            var targetGap = profile.TargetWeight.HasValue ? profile.TargetWeight.Value - weight : 0m;

            var calories = maintenance;
            if (isWeightLoss || targetGap < -1m)
                calories -= targetGap < -10m ? 500m : 350m;
            else if (isMuscleGain || targetGap > 1m)
                calories += targetGap > 8m ? 400m : 250m;

            if (hasDiabetes && calories < maintenance - 450m)
                calories = maintenance - 450m;

            calories = Math.Round(Math.Clamp(calories, isFemale ? 1200m : 1400m, 4200m) / 25m) * 25m;
            var proteinMultiplier = isMuscleGain ? 2.0m : isWeightLoss ? 1.8m : 1.6m;
            var protein = Math.Round(weight * proteinMultiplier);
            var fat = Math.Round(Math.Max(weight * 0.7m, calories * 0.22m / 9m));
            var carbs = Math.Round(Math.Max((calories - protein * 4m - fat * 9m) / 4m, hasDiabetes ? 90m : 120m));

            var meals = CreatePersonalizedFallbackMeals(calories, protein, carbs, fat, hasDiabetes, isVegetarian);

            return new NutritionPlanAIResponse
            {
                DailyCalories = calories,
                ProteinGrams = protein,
                CarbsGrams = carbs,
                FatGrams = fat,
                Meals = meals
            };
        }

        private static List<MealAIResponse> CreatePersonalizedFallbackMeals(
            decimal dailyCalories,
            decimal protein,
            decimal carbs,
            decimal fat,
            bool hasDiabetes,
            bool isVegetarian)
        {
            var proteinBase = isVegetarian ? "Greek yogurt, eggs, lentils, chickpeas, tofu" : "eggs, Greek yogurt, chicken, fish";
            var carbBase = hasDiabetes ? "oats, sweet potato, vegetables, brown rice" : "oats, rice, potatoes, fruit, vegetables";
            var breakfastCalories = Math.Round(dailyCalories * 0.25m);
            var lunchCalories = Math.Round(dailyCalories * 0.35m);
            var dinnerCalories = Math.Round(dailyCalories * 0.30m);
            var snackCalories = dailyCalories - breakfastCalories - lunchCalories - dinnerCalories;

            return
            [
                CreateMeal("Breakfast", hasDiabetes ? "Low-GI protein breakfast" : "Protein breakfast", breakfastCalories, protein * 0.25m, carbs * 0.28m, fat * 0.22m, $"{proteinBase}, {carbBase}, chia or nuts"),
                CreateMeal("Lunch", isVegetarian ? "Legume power bowl" : "Lean protein bowl", lunchCalories, protein * 0.35m, carbs * 0.36m, fat * 0.32m, $"{proteinBase}, {carbBase}, salad, olive oil"),
                CreateMeal("Dinner", hasDiabetes ? "Steady blood sugar dinner" : "Recovery dinner", dinnerCalories, protein * 0.30m, carbs * 0.26m, fat * 0.34m, $"{proteinBase}, vegetables, {carbBase}"),
                CreateMeal("Snack", "Goal-support snack", snackCalories, protein * 0.10m, carbs * 0.10m, fat * 0.12m, isVegetarian ? "Cottage cheese or hummus with vegetables" : "Protein shake or yogurt with nuts")
            ];
        }

        private static MealAIResponse CreateMeal(
            string type,
            string name,
            decimal calories,
            decimal protein,
            decimal carbs,
            decimal fat,
            string ingredients) => new()
            {
                MealType = type,
                Name = name,
                Calories = Math.Round(calories),
                ProteinGrams = Math.Round(protein),
                CarbsGrams = Math.Round(carbs),
                FatGrams = Math.Round(fat),
                Ingredients = ingredients
            };

        private static int CalculateAge(DateTime dateOfBirth)
        {
            var today = DateTime.UtcNow.Date;
            var age = today.Year - dateOfBirth.Year;
            if (dateOfBirth.Date > today.AddYears(-age))
                age--;
            return Math.Clamp(age, 13, 90);
        }

        private static decimal GetActivityMultiplier(string? activityLevel)
        {
            if (ContainsAny(activityLevel, "sedentary")) return 1.2m;
            if (ContainsAny(activityLevel, "light")) return 1.375m;
            if (ContainsAny(activityLevel, "active", "very")) return 1.725m;
            if (ContainsAny(activityLevel, "moderate")) return 1.55m;
            return 1.4m;
        }

        private static bool IsWeightLossGoal(ArenaDomain.Entities.MemberProfile profile) =>
            ContainsAny(profile.Goal, "loss", "lose", "cut", "اخس", "تنشيف")
            || (profile.TargetWeight.HasValue && profile.Weight.HasValue && profile.TargetWeight.Value < profile.Weight.Value - 1m);

        private static bool ContainsAny(string? text, params string[] values)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            return values.Any(value => text.Contains(value, StringComparison.OrdinalIgnoreCase));
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

        private static string BuildHealthAwareUserMessage(string userMessage, string healthContext)
        {
            if (string.IsNullOrWhiteSpace(healthContext))
                return userMessage;

            return $"""
            {userMessage}

            === MEMBER'S KNOWN HEALTH HISTORY (CRITICAL - MUST RESPECT) ===
            {healthContext}
            """;
        }

        private static void ApplyLocalNutritionReplacements(NutritionPlanAIResponse plan, string healthContext)
        {
            if (plan?.Meals == null) return;

            bool hasPeanuts = WorkoutLocalization.ContainsAny(healthContext, "peanut", "فول سوداني");
            bool hasLactose = WorkoutLocalization.ContainsAny(healthContext, "lactose", "dairy", "milk", "cheese", "yogurt", "حليب", "جبن", "لبن");

            foreach (var meal in plan.Meals)
            {
                if (hasPeanuts)
                {
                    meal.Name = ReplacePeanuts(meal.Name);
                    meal.Ingredients = ReplacePeanuts(meal.Ingredients);
                }
                if (hasLactose)
                {
                    meal.Name = ReplaceDairy(meal.Name);
                    meal.Ingredients = ReplaceDairy(meal.Ingredients);
                }
            }
        }

        private static string ReplacePeanuts(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text
                .Replace("peanut butter", "almond butter", StringComparison.OrdinalIgnoreCase)
                .Replace("peanut", "almond", StringComparison.OrdinalIgnoreCase)
                .Replace("peanuts", "almonds", StringComparison.OrdinalIgnoreCase)
                .Replace("زبدة الفول السوداني", "زبدة اللوز", StringComparison.OrdinalIgnoreCase)
                .Replace("فول سوداني", "لوز", StringComparison.OrdinalIgnoreCase);
        }

        private static string ReplaceDairy(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text
                .Replace("greek yogurt", "lactose-free yogurt", StringComparison.OrdinalIgnoreCase)
                .Replace("yogurt", "lactose-free yogurt", StringComparison.OrdinalIgnoreCase)
                .Replace("milk", "almond milk", StringComparison.OrdinalIgnoreCase)
                .Replace("cheese", "tofu", StringComparison.OrdinalIgnoreCase)
                .Replace("cottage cheese", "tofu", StringComparison.OrdinalIgnoreCase)
                .Replace("زبادي يوناني", "زبادي خالي من اللاكتوز", StringComparison.OrdinalIgnoreCase)
                .Replace("زبادي", "زبادي خالي من اللاكتوز", StringComparison.OrdinalIgnoreCase)
                .Replace("حليب", "حليب اللوز", StringComparison.OrdinalIgnoreCase)
                .Replace("لبن", "حليب اللوز", StringComparison.OrdinalIgnoreCase)
                .Replace("جبن قريش", "توفو", StringComparison.OrdinalIgnoreCase)
                .Replace("جبن", "توفو", StringComparison.OrdinalIgnoreCase);
        }

        private static void LocalizePlanData(NutritionPlanAIResponse planData, bool isArabic)
        {
            if (isArabic && planData.Meals != null)
            {
                foreach (var meal in planData.Meals)
                {
                    meal.MealType = WorkoutLocalization.TranslateMealType(meal.MealType);
                    meal.Name = WorkoutLocalization.TranslatePhrase(meal.Name);
                    meal.Ingredients = WorkoutLocalization.TranslatePhrase(meal.Ingredients);
                }
            }
        }
    }
}