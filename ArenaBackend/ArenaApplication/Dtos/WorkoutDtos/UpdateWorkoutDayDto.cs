using System;

namespace ArenaApplication.Dtos.WorkoutDtos
{
    public class UpdateWorkoutDayDto
    {
        public int DayNumber { get; set; }

        public string DayName { get; set; } = string.Empty;
    }
}
