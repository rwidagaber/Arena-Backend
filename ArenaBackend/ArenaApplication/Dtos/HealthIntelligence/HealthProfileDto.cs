using System.Collections.Generic;

namespace ArenaApplication.Dtos.HealthIntelligence
{
    public class HealthProfileDto
    {
        public List<string> Conditions { get; set; } = new();
        public List<string> Allergies { get; set; } = new();
        public List<string> Injuries { get; set; } = new();
        public List<string> Restrictions { get; set; } = new();
        public List<string> Medications { get; set; } = new();

        // Additional profile metadata stored in HealthProfileJson to avoid database migrations
        public decimal? BodyFat { get; set; }
        public int? SleepHours { get; set; }
        public string? DailySchedule { get; set; }
        public string? PreferredWorkoutTime { get; set; }
        public string? TrainerNotes { get; set; }
        public string? Lifestyle { get; set; }
        public string? FoodPreferences { get; set; }
        public string? PhysicalLimitations { get; set; }
        public string? ChronicDiseases { get; set; }
        public string? PreferredWorkoutDays { get; set; }
        public int? PreferredWorkoutDuration { get; set; }
    }
}
