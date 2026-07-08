using ArenaApplication.AI.Planning;
using ArenaApplication.Dtos.ChatDtos;
using ArenaApplication.Dtos.HealthIntelligence;
using ArenaApplication.AI;
using ArenaApplication.IServices;
using ArenaDomain.Entities;
using ArenaDomain.Entities.Bookings;
using ArenaDomain.Entities.Health;
using ArenaDomain.Entities.Nutrition;
using ArenaDomain.Entities.Subscription;
using ArenaDomain.Entities.Workout;
using ArenaDomain.Enums;
using ArenaInfrastructure.Data;
using ArenaInfrastructure.AI;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ArenaInfrastructure.AI.Planning.Steps
{
    public class AnalyzeUserAndMessageStep : IPlanningStep
    {
        private readonly AppDbContext _context;
        private readonly IGeminiCompletionService _gemini;

        public AnalyzeUserAndMessageStep(AppDbContext context, IGeminiCompletionService gemini)
        {
            _context = context;
            _gemini = gemini;
        }

        public async Task ExecuteAsync(PlanningContext context)
        {
            // 1. Fetch member profile
            var profile = await _context.MemberProfiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == context.MemberProfileId || p.UserId == context.MemberProfileId);

            if (profile == null)
            {
                throw new Exception($"Profile not found: {context.MemberProfileId}");
            }

            context.Profile = profile;

            // 2. Fetch recent progress logs
            context.ProgressLogs = await _context.ProgressLogs
                .Where(log => log.MemberProfileId == profile.Id)
                .OrderByDescending(log => log.LoggedAt)
                .Take(8)
                .OrderBy(log => log.LoggedAt)
                .ToListAsync();

            // 3. Fetch recent workout plans
            context.WorkoutPlans = await _context.WorkoutPlans
                .Where(plan => plan.MemberProfileId == profile.Id && !plan.IsDeleted)
                .Include(plan => plan.WorkoutDays)
                .OrderByDescending(plan => plan.CreatedAt)
                .Take(5)
                .ToListAsync();

            // 4. Fetch recent nutrition plans
            context.NutritionPlans = await _context.NutritionPlans
                .Where(plan => plan.MemberProfileId == profile.Id && !plan.IsDeleted)
                .Include(plan => plan.Meals)
                .OrderByDescending(plan => plan.CreatedAt)
                .Take(5)
                .ToListAsync();

            // 5. Fetch subscription
            context.ActiveSubscription = await _context.UserSubscriptions
                .FirstOrDefaultAsync(s => s.MemberProfileId == profile.Id && s.Status == SubscriptionStatus.Active && s.EndDate > DateTime.UtcNow);

            // 6. Fetch attendances
            context.Attendances = await _context.Attendances
                .Where(a => a.MemberProfileId == profile.Id)
                .OrderByDescending(a => a.CheckInTime)
                .Take(10)
                .ToListAsync();

            // 7. Load structured health/profile metadata from HealthProfileJson
            if (!string.IsNullOrWhiteSpace(profile.HealthProfileJson))
            {
                try
                {
                    context.HealthProfile = JsonSerializer.Deserialize<HealthProfileDto>(
                        profile.HealthProfileJson, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new HealthProfileDto();
                }
                catch
                {
                    context.HealthProfile = new HealthProfileDto();
                }
            }

            // 8. Build UserContextText
            context.UserContextText = UserContextBuilder.Build(
                profile,
                context.ActiveSubscription,
                recentProgress: context.ProgressLogs,
                nutritionPlans: context.NutritionPlans,
                workoutPlans: context.WorkoutPlans,
                attendanceHistory: context.Attendances);

            // 9. Extract user preferences naturally from user message using LLM
            if (!string.IsNullOrWhiteSpace(context.UserMessage))
            {
                var extractionPrompt = """
You are a highly advanced fitness coach message parser.
Analyze the user's current message and extract any preferences, goals, duration constraints, equipment constraints, health issues, or lifestyle parameters mentioned.

Return ONLY a valid JSON object in this exact format:
{
  "goal": "Weight Loss" (or "Muscle Gain", "Chest Hypertrophy", "Strength", "Endurance", "Athletic Performance", "Rehabilitation", "Maintain Weight", "Improve Fitness", "Increase Flexibility" or null),
  "duration": "8 weeks" (e.g. "4 weeks", "3 months" or null),
  "equipment": "Dumbbells" (e.g. "Dumbbells", "Full Gym", "Bodyweight" or null),
  "workoutFrequency": "3 days" (e.g. "3 days/week", "4 days" or null),
  "timePerWorkout": "30 minutes" (or null),
  "injuriesOrPain": "Knee pain" (or null),
  "dietaryRestrictions": "Vegan" (or null),
  "foodPreferences": "No peanuts" (or null),
  "allergies": "Peanuts" (or null),
  "diseasesOrConditions": "Diabetic" (or null),
  "medications": "Insulin" (or null),
  "lifestyle": "Sedentary office worker" (or null),
  "sleepHours": 6 (or null),
  "dailySchedule": "Busy daily schedule" (or null),
  "preferredWorkoutTime": "Morning" (or null),
  "physicalLimitations": "Left knee issues" (or null),
  "chronicDiseases": "Type 2 diabetes" (or null),
  "preferredWorkoutDays": "Mon, Wed, Fri" (or null),
  "preferredWorkoutDuration": 45 (workout duration in minutes, or null)
}
""";

                try
                {
                    var response = await _gemini.GetCompletionAsync(extractionPrompt, new List<ChatMessageDto>(), context.UserMessage);
                    var cleanJson = AIHelper.CleanJson(response);
                    
                    using (var doc = JsonDocument.Parse(cleanJson))
                    {
                        var root = doc.RootElement;
                        
                        // Parse values into Context
                        context.MessagePreferences = cleanJson;

                        // Merge extracted details into context.HealthProfile (temporary merge, will be saved if validated)
                        UpdateHealthProfileFromExtraction(context.HealthProfile, root);

                        profile.HealthProfileJson = JsonSerializer.Serialize(context.HealthProfile);
                        _context.MemberProfiles.Update(profile);
                        await _context.SaveChangesAsync();
                    }
                }
                catch
                {
                    // Fallback silently if LLM parsing fails
                }
            }
        }

        private void UpdateHealthProfileFromExtraction(HealthProfileDto hp, JsonElement root)
        {
            if (hp == null) return;

            // Merge Conditions
            if (root.TryGetProperty("diseasesOrConditions", out var val) && val.ValueKind == JsonValueKind.String)
            {
                var str = val.GetString();
                if (!string.IsNullOrWhiteSpace(str) && !hp.Conditions.Contains(str, StringComparer.OrdinalIgnoreCase))
                    hp.Conditions.Add(str);
            }
            if (root.TryGetProperty("chronicDiseases", out val) && val.ValueKind == JsonValueKind.String)
            {
                var str = val.GetString();
                if (!string.IsNullOrWhiteSpace(str) && !hp.Conditions.Contains(str, StringComparer.OrdinalIgnoreCase))
                    hp.Conditions.Add(str);
            }

            // Merge Allergies
            if (root.TryGetProperty("allergies", out val) && val.ValueKind == JsonValueKind.String)
            {
                var str = val.GetString();
                if (!string.IsNullOrWhiteSpace(str) && !hp.Allergies.Contains(str, StringComparer.OrdinalIgnoreCase))
                    hp.Allergies.Add(str);
            }

            // Merge Injuries
            if (root.TryGetProperty("injuriesOrPain", out val) && val.ValueKind == JsonValueKind.String)
            {
                var str = val.GetString();
                if (!string.IsNullOrWhiteSpace(str) && !hp.Injuries.Contains(str, StringComparer.OrdinalIgnoreCase))
                    hp.Injuries.Add(str);
            }

            // Merge Restrictions
            if (root.TryGetProperty("dietaryRestrictions", out val) && val.ValueKind == JsonValueKind.String)
            {
                var str = val.GetString();
                if (!string.IsNullOrWhiteSpace(str) && !hp.Restrictions.Contains(str, StringComparer.OrdinalIgnoreCase))
                    hp.Restrictions.Add(str);
            }

            // Merge Medications
            if (root.TryGetProperty("medications", out val) && val.ValueKind == JsonValueKind.String)
            {
                var str = val.GetString();
                if (!string.IsNullOrWhiteSpace(str) && !hp.Medications.Contains(str, StringComparer.OrdinalIgnoreCase))
                    hp.Medications.Add(str);
            }

            // Merge FoodPreferences
            if (root.TryGetProperty("foodPreferences", out val) && val.ValueKind == JsonValueKind.String)
            {
                var str = val.GetString();
                if (!string.IsNullOrWhiteSpace(str))
                    hp.FoodPreferences = string.IsNullOrWhiteSpace(hp.FoodPreferences) ? str : hp.FoodPreferences + ", " + str;
            }

            // Merge PhysicalLimitations
            if (root.TryGetProperty("physicalLimitations", out val) && val.ValueKind == JsonValueKind.String)
            {
                var str = val.GetString();
                if (!string.IsNullOrWhiteSpace(str))
                    hp.PhysicalLimitations = string.IsNullOrWhiteSpace(hp.PhysicalLimitations) ? str : hp.PhysicalLimitations + ", " + str;
            }

            // Merge SleepHours
            if (root.TryGetProperty("sleepHours", out val) && val.ValueKind == JsonValueKind.Number)
            {
                hp.SleepHours = val.GetInt32();
            }

            // Merge DailySchedule
            if (root.TryGetProperty("dailySchedule", out val) && val.ValueKind == JsonValueKind.String)
            {
                hp.DailySchedule = val.GetString();
            }

            // Merge PreferredWorkoutTime
            if (root.TryGetProperty("preferredWorkoutTime", out val) && val.ValueKind == JsonValueKind.String)
            {
                hp.PreferredWorkoutTime = val.GetString();
            }

            // Merge Lifestyle
            if (root.TryGetProperty("lifestyle", out val) && val.ValueKind == JsonValueKind.String)
            {
                hp.Lifestyle = val.GetString();
            }

            // Merge PreferredWorkoutDays
            if (root.TryGetProperty("preferredWorkoutDays", out val) && val.ValueKind == JsonValueKind.String)
            {
                hp.PreferredWorkoutDays = val.GetString();
            }

            // Merge PreferredWorkoutDuration
            if (root.TryGetProperty("preferredWorkoutDuration", out val) && val.ValueKind == JsonValueKind.Number)
            {
                hp.PreferredWorkoutDuration = val.GetInt32();
            }
            else if (root.TryGetProperty("timePerWorkout", out val) && val.ValueKind == JsonValueKind.String)
            {
                var timeStr = val.GetString();
                if (!string.IsNullOrWhiteSpace(timeStr))
                {
                    var digits = new string(timeStr.Where(char.IsDigit).ToArray());
                    if (int.TryParse(digits, out var mins))
                    {
                        hp.PreferredWorkoutDuration = mins;
                    }
                }
            }
        }
    }
}
