using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Dtos.WorkoutDtos
{
    public class WorkoutDayDto
    {
        public Guid Id { get; set; }

        public Guid WorkoutPlanId { get; set; }

        public int DayNumber { get; set; }

        public string DayName { get; set; } = string.Empty;

        public List<WorkoutExerciseDto> Exercises { get; set; } = new();
    }
}
