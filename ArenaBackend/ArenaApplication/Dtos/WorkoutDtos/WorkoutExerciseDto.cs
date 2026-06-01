using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Dtos.WorkoutDtos
{
    public class WorkoutExerciseDto
    {
        public Guid Id { get; set; }

        public Guid WorkoutDayId { get; set; }

        public Guid ExerciseId { get; set; }

        // Optional nested exercise info; can be null when not included
        public ExerciseDto? Exercise { get; set; }

        public int Sets { get; set; }

        public int Reps { get; set; }
        public string Name { get; set; } = string.Empty;

        public decimal? Weight { get; set; }

        public int? DurationMinutes { get; set; }

        public int? RestSeconds { get; set; }

        public string? Notes { get; set; }
    }
}
