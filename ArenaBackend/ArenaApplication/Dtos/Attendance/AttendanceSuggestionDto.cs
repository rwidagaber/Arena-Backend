using System;
using System.Collections.Generic;

namespace ArenaApplication.Dtos.Attendance
{
    /// <summary>
    /// Recommendation for the least-busy time(s) to attend the gym on a date.
    /// </summary>
    public class AttendanceSuggestionDto
    {
        public DateTime Date { get; set; }

        public string DayOfWeek { get; set; } = string.Empty;

        public bool IsClosed { get; set; }

        /// <summary>Least-busy open hours (local 24h), best first.</summary>
        public List<int> RecommendedHours { get; set; } = new();

        /// <summary>Friendly recommendation text (AI-generated, or data fallback).</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>True when the message came from the AI; false for the data fallback.</summary>
        public bool AiGenerated { get; set; }

        /// <summary>The hourly occupancy profile the recommendation was based on.</summary>
        public DayOccupancyDto Occupancy { get; set; } = new();
    }
}
