using System;

namespace ArenaApplication.Dtos.WorkoutDtos
{
    public class CreateWorkoutDayDto
    {
        public Guid WorkoutPlanId { get; set; }

        public int DayNumber { get; set; }

        public string DayName { get; set; } = string.Empty;
    }
}
