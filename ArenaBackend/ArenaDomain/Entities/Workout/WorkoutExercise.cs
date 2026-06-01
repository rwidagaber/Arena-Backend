using ArenaDomain.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaDomain.Entities.Workout
{
    public class WorkoutExercise : BaseEntity<Guid>
    {
        public Guid WorkoutDayId { get; set; }

        public virtual WorkoutDay WorkoutDay { get; set; } = null!;
        public string ExName { get; set; }
        public Guid ExerciseId { get; set; }

        public virtual Exercise Exercise { get; set; } = null!;

        public int Sets { get; set; }

        public int Reps { get; set; }

        public decimal? Weight { get; set; }

        public int? DurationMinutes { get; set; }

        public int? RestSeconds { get; set; }

        public string? Notes { get; set; }
    }
}
