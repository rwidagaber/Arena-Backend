using System;

namespace ArenaApplication.Dtos.WorkoutDtos
{
    public class CreateWorkoutExerciseDto
    {
        public Guid WorkoutDayId { get; set; }

        public Guid ExerciseId { get; set; }

        public int Sets { get; set; }

        public int Reps { get; set; }

        public decimal? Weight { get; set; }

        public int? DurationMinutes { get; set; }

        public int? RestSeconds { get; set; }

        public string? Notes { get; set; }
    }
}
