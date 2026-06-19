using ArenaDomain.Entities;
using ArenaDomain.Entities.Bookings;
using ArenaDomain.Entities.Subscription;
using ArenaDomain.Entities.User;

namespace ArenaApplication.AI
{
    public static class UserContextBuilder
    {
        public static string Build(

            MemberProfile profile,
            UserSubscription? subscription = null,
            List<Booking>? todayBookings = null,
            List<Booking>? upcomingBookings = null,
            object? recentProgress = null)
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
                
                === FITNESS INFO ===
                Goal: {profile.Goal ?? "General Fitness"}
                Activity Level: {profile.ActivityLevel ?? "Moderate"}
                Experience: {profile.FitnessExperience ?? "Beginner"}
                Available Equipment: {profile.Equipment ?? "Full Gym"}
                
                === HEALTH & RESTRICTIONS ===
                Health Conditions: {profile.HealthConditions ?? "None"}
                Injuries: {profile.Injuries ?? "None"}
                Dietary Restrictions: {profile.DietaryRestrictions ?? "None"}
                """;

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
                    .Select(g => $"{g.Key}:00 → {g.Count()} people")
                    .Aggregate((a, b) => a + "\n" + b);

                context += slots;

                var crowdLevel = todayBookings.Count switch
                {
                    < 5 => "🟢 Not busy",
                    < 10 => "🟡 Moderate",
                    _ => "🔴 Very busy"
                };
                context += $"\nCurrent crowd level: {crowdLevel}";
            }

            return context;
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
    }
}
