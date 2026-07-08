using ArenaDomain.Entities;
using ArenaDomain.Entities.Bookings;
using ArenaDomain.Entities.Health;
using ArenaDomain.Entities.Nutrition;
using ArenaDomain.Entities.Subscription;
using ArenaDomain.Entities.User;
using ArenaDomain.Entities.Workout;
using ArenaApplication.Dtos.HealthIntelligence;
using System.Globalization;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

namespace ArenaApplication.AI
{
    public static class UserContextBuilder
    {
        public static string Build(
            MemberProfile profile,
            UserSubscription? subscription = null,
            List<Booking>? todayBookings = null,
            List<Booking>? upcomingBookings = null,
            IReadOnlyList<ProgressLog>? recentProgress = null,
            IReadOnlyList<NutritionPlan>? nutritionPlans = null,
            IReadOnlyList<WorkoutPlan>? workoutPlans = null,
            IReadOnlyList<Attendance>? attendanceHistory = null)
        {
            var age = DateTime.UtcNow.Year - profile.DateOfBirth.Year;
            var bmi = profile.BMI ?? CalculateBMI(profile.Weight, profile.Height);
            var firstName = string.IsNullOrWhiteSpace(profile.User?.FirstName)
                ? profile.FirstName
                : profile.User.FirstName;
            var fullName = string.Join(" ", new[]
            {
                profile.User?.FirstName,
                profile.User?.LastName
            }.Where(value => !string.IsNullOrWhiteSpace(value)));

            HealthProfileDto? healthProfile = null;
            if (!string.IsNullOrWhiteSpace(profile.HealthProfileJson))
            {
                try
                {
                    healthProfile = JsonSerializer.Deserialize<HealthProfileDto>(profile.HealthProfileJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch { }
            }

            var sleepHours = healthProfile?.SleepHours?.ToString() ?? "Not set";
            var dailySchedule = healthProfile?.DailySchedule ?? "Not set";
            var preferredWorkoutTime = healthProfile?.PreferredWorkoutTime ?? "Not set";
            var trainerNotes = healthProfile?.TrainerNotes ?? "None";
            var lifestyle = healthProfile?.Lifestyle ?? "Not set";
            var foodPreferences = healthProfile?.FoodPreferences ?? "None";
            var physicalLimitations = healthProfile?.PhysicalLimitations ?? "None";
            var chronicDiseases = healthProfile?.ChronicDiseases ?? "None";
            var profileBodyFat = healthProfile?.BodyFat?.ToString("0.#") ?? "Not set";
            var preferredDays = healthProfile?.PreferredWorkoutDays ?? "Not set";
            var preferredDuration = healthProfile?.PreferredWorkoutDuration?.ToString() ?? "Not set";

            var attendanceSummary = "No attendance logs found";
            if (attendanceHistory != null && attendanceHistory.Any())
            {
                attendanceSummary = string.Join(", ", attendanceHistory.Take(10).Select(a => $"{a.CheckInTime:yyyy-MM-dd} (Attended)"));
            }

            var context = $"""
                === MEMBER PROFILE ===
                Application First Name: {firstName ?? "Member"}
                Application Full Name: {(string.IsNullOrWhiteSpace(fullName) ? firstName ?? "Member" : fullName)}
                Preferred Language: {profile.User?.PreferredLanguage ?? "Not set"}
                Age: {age}
                Gender: {profile.Gender}
                Weight: {profile.Weight ?? 0}kg
                Height: {profile.Height ?? 0}cm
                BMI: {bmi:F1} ({GetBMICategory(bmi)})
                Target Weight: {FormatDecimal(profile.TargetWeight, "Not set")}
                Muscle Mass: {FormatDecimal(profile.MuscleMass, "Not set")}
                Body Fat: {profileBodyFat}%
                Lifestyle: {lifestyle}
                Sleep Hours: {sleepHours}
                Daily Schedule: {dailySchedule}
                Preferred Workout Time: {preferredWorkoutTime}
                Trainer Notes: {trainerNotes}
                
                === FITNESS INFO ===
                Goal: {profile.Goal ?? "General Fitness"}
                Activity Level: {profile.ActivityLevel ?? "Moderate"}
                Experience: {profile.FitnessExperience ?? "Beginner"}
                Available Equipment: {profile.Equipment ?? "Full Gym"}
                Preferred Workout Days: {preferredDays}
                Preferred Workout Duration: {preferredDuration} minutes
                
                === HEALTH & RESTRICTIONS ===
                Health Conditions: {profile.HealthConditions ?? "None"}
                Chronic Diseases: {chronicDiseases}
                Injuries: {profile.Injuries ?? "None"}
                Physical Limitations: {physicalLimitations}
                Dietary Restrictions: {profile.DietaryRestrictions ?? "None"}
                Allergies: {(healthProfile != null && healthProfile.Allergies.Any() ? string.Join(", ", healthProfile.Allergies) : "None")}
                Medications: {(healthProfile != null && healthProfile.Medications.Any() ? string.Join(", ", healthProfile.Medications) : "None")}
                Food Preferences: {foodPreferences}

                === ATTENDANCE HISTORY ===
                {attendanceSummary}
                """;

            context += BuildProgressContext(profile, recentProgress);
            context += BuildPlanHistoryContext(nutritionPlans, workoutPlans);


            if (subscription != null)
            {
                context += $"""

                === SUBSCRIPTION ===
                Plan: Active
                Remaining Sessions: {subscription.RemainingSessions}
                Expires: {subscription.EndDate:yyyy-MM-dd}
                """;
            }

            if (todayBookings != null && todayBookings.Any())
            {
                context += "\n=== TODAY'S GYM SCHEDULE ===\n";
                context += $"Total bookings today: {todayBookings.Count}\n";

                var slots = todayBookings
                    .GroupBy(b => b.StartTime.Hours)
                    .Select(g => $"{g.Key}:00 -> {g.Count()} people")
                    .Aggregate((a, b) => a + "\n" + b);

                context += slots;

                var crowdLevel = todayBookings.Count switch
                {
                    < 5 => "Not busy",
                    < 10 => "Moderate",
                    _ => "Very busy"
                };
                context += $"\nCurrent crowd level: {crowdLevel}";
            }

            return context;
        }


        private static string BuildPlanHistoryContext(
            IReadOnlyList<NutritionPlan>? nutritionPlans,
            IReadOnlyList<WorkoutPlan>? workoutPlans)
        {
            var lines = new List<string>
            {
                string.Empty,
                "=== MEMBER PLAN HISTORY ==="
            };

            if (nutritionPlans == null || nutritionPlans.Count == 0)
            {
                lines.Add("Nutrition Plans: None saved yet");
            }
            else
            {
                var orderedNutritionPlans = nutritionPlans
                    .OrderByDescending(plan => plan.IsActive)
                    .ThenByDescending(plan => plan.CreatedAt)
                    .ToList();
                var latestNutrition = orderedNutritionPlans
                    .OrderByDescending(plan => plan.CreatedAt)
                    .First();
                var activeNutrition = orderedNutritionPlans.FirstOrDefault(plan => plan.IsActive);

                lines.Add($"Nutrition Plans Count: {orderedNutritionPlans.Count}");
                lines.Add($"Latest Nutrition Plan: {FormatNutritionPlan(latestNutrition)}");
                lines.Add($"Active Nutrition Plan User Follows: {(activeNutrition == null ? "None" : FormatNutritionPlan(activeNutrition))}");
                lines.Add("Recent Nutrition Plans:");
                lines.AddRange(orderedNutritionPlans.Take(5).Select(plan => $"- {FormatNutritionPlan(plan)}"));
            }

            if (workoutPlans == null || workoutPlans.Count == 0)
            {
                lines.Add("Workout Plans: None saved yet");
            }
            else
            {
                var orderedWorkoutPlans = workoutPlans
                    .OrderByDescending(plan => plan.IsActive)
                    .ThenByDescending(plan => plan.CreatedAt)
                    .ToList();
                var latestWorkout = orderedWorkoutPlans
                    .OrderByDescending(plan => plan.CreatedAt)
                    .First();
                var activeWorkout = orderedWorkoutPlans.FirstOrDefault(plan => plan.IsActive);

                lines.Add($"Workout Plans Count: {orderedWorkoutPlans.Count}");
                lines.Add($"Latest Workout Plan: {FormatWorkoutPlan(latestWorkout)}");
                lines.Add($"Active Workout Plan User Follows: {(activeWorkout == null ? "None" : FormatWorkoutPlan(activeWorkout))}");
                lines.Add("Recent Workout Plans:");
                lines.AddRange(orderedWorkoutPlans.Take(5).Select(plan => $"- {FormatWorkoutPlan(plan)}"));
            }

            return Environment.NewLine + string.Join(Environment.NewLine, lines);
        }

        private static string BuildProgressContext(MemberProfile profile, IReadOnlyList<ProgressLog>? recentProgress)
        {
            if (recentProgress == null || recentProgress.Count == 0)
            {
                return $"""

                === TRACKED PROGRESS ===
                Progress Logs: None yet
                Current Known Weight: {FormatDecimal(profile.Weight, "Unknown")}
                Current Known Muscle Mass: {FormatDecimal(profile.MuscleMass, "Unknown")}
                Progress Guidance: Use profile baseline only. Start conservatively and ask the member to log weight, body fat, and muscle mass weekly.
                """;
            }

            var orderedLogs = recentProgress.OrderBy(log => log.LoggedAt).ToList();
            var first = orderedLogs.First();
            var latest = orderedLogs.Last();
            var previous = orderedLogs.Count > 1 ? orderedLogs[^2] : null;
            var totalWeightChange = latest.Weight - first.Weight;
            var recentWeightChange = previous == null ? null : (decimal?)(latest.Weight - previous.Weight);
            var totalMuscleChange = first.MuscleMass.HasValue && latest.MuscleMass.HasValue
                ? latest.MuscleMass.Value - first.MuscleMass.Value
                : (decimal?)null;
            var totalBodyFatChange = first.BodyFat.HasValue && latest.BodyFat.HasValue
                ? latest.BodyFat.Value - first.BodyFat.Value
                : (decimal?)null;

            var latestEntries = orderedLogs
                .TakeLast(5)
                .Select(log => $"- {log.LoggedAt:yyyy-MM-dd}: weight {FormatDecimal(log.Weight, "Unknown")}, body fat {FormatDecimal(log.BodyFat, "Unknown")}, muscle mass {FormatDecimal(log.MuscleMass, "Unknown")}");

            return $"""

            === TRACKED PROGRESS ===
            Progress Logs Count: {orderedLogs.Count}
            Latest Log Date: {latest.LoggedAt:yyyy-MM-dd}
            Latest Weight: {FormatDecimal(latest.Weight, "Unknown")}
            Latest Body Fat: {FormatDecimal(latest.BodyFat, "Unknown")}
            Latest Muscle Mass: {FormatDecimal(latest.MuscleMass, "Unknown")}
            Total Weight Change In Logged Period: {FormatSignedDecimal(totalWeightChange, "Unknown")}
            Recent Weight Change Since Previous Log: {FormatSignedDecimal(recentWeightChange, "Only one log")}
            Total Body Fat Change In Logged Period: {FormatSignedDecimal(totalBodyFatChange, "Unknown")}
            Total Muscle Mass Change In Logged Period: {FormatSignedDecimal(totalMuscleChange, "Unknown")}
            Target Weight Gap: {FormatTargetWeightGap(profile.TargetWeight, latest.Weight)}
            Recent Logs:
            {string.Join("\n", latestEntries)}
            """;
        }

        private static decimal CalculateBMI(decimal? weight, decimal? height)
        {
            if (weight == null || height == null || height == 0) return 0;
            var heightM = height.Value / 100;
            return weight.Value / (heightM * heightM);
        }

        private static string GetBMICategory(decimal bmi) => bmi switch
        {
            < 18.5m => "Underweight",
            < 25m => "Normal",
            < 30m => "Overweight",
            _ => "Obese"
        };

        private static string FormatDecimal(decimal? value, string fallback) =>
            value.HasValue ? value.Value.ToString("0.##", CultureInfo.InvariantCulture) : fallback;

        private static string FormatSignedDecimal(decimal? value, string fallback) =>
            value.HasValue ? value.Value.ToString("+0.##;-0.##;0", CultureInfo.InvariantCulture) : fallback;

        private static string FormatTargetWeightGap(decimal? targetWeight, decimal latestWeight)
        {
            if (!targetWeight.HasValue)
                return "No target weight set";

            var gap = targetWeight.Value - latestWeight;
            return $"{gap.ToString("+0.##;-0.##;0", CultureInfo.InvariantCulture)}kg to target";
        }


        private static string FormatNutritionPlan(NutritionPlan plan)
        {
            var meals = plan.Meals?
                .Take(4)
                .Select(meal => $"{meal.MealType}: {meal.Name}")
                .ToList() ?? [];

            return $"{plan.CreatedAt:yyyy-MM-dd} ({(plan.IsActive ? "active" : "inactive")}): {FormatDecimal(plan.DailyCalories, "0")} kcal, P {FormatDecimal(plan.ProteinGrams, "0")}g, C {FormatDecimal(plan.CarbsGrams, "0")}g, F {FormatDecimal(plan.FatGrams, "0")}g"
                + (meals.Count == 0 ? "" : $"; meals: {string.Join("; ", meals)}");
        }

        private static string FormatWorkoutPlan(WorkoutPlan plan)
        {
            var days = plan.WorkoutDays?
                .Take(4)
                .Select(day => day.DayName)
                .ToList() ?? [];

            return $"{plan.CreatedAt:yyyy-MM-dd} ({(plan.IsActive ? "active" : "inactive")}): {plan.Name}, {plan.DurationWeeks} weeks"
                + (days.Count == 0 ? "" : $"; days: {string.Join(", ", days)}");
        }

    }
}
