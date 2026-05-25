using System;

namespace ArenaApplication.Dtos.WorkoutDtos
{
    public class CreateExerciseDto
    {
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string MuscleGroup { get; set; } = string.Empty;

        public string Equipment { get; set; } = string.Empty;

        public string? VideoUrl { get; set; }

        public string? ImageUrl { get; set; }

        public Guid MemberProfileId { get; set; }
    }
}
