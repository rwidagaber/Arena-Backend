using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Dtos.WorkoutDtos
{
    public class ExerciseDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string? NameAr { get; set; }

        public string Description { get; set; } = string.Empty;
        public string? DescriptionAr { get; set; }

        public string MuscleGroup { get; set; } = string.Empty;
        public string? MuscleGroupAr { get; set; }

        public string Equipment { get; set; } = string.Empty;
        public string? EquipmentAr { get; set; }

        public string? VideoUrl { get; set; }

        public string? ImageUrl { get; set; }

        public Guid MemberProfileId { get; set; }
    }
}
