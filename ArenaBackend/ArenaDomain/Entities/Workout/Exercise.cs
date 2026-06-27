using ArenaDomain.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaDomain.Entities.Workout
{
    public class Exercise : BaseEntity<Guid>
    {
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

        // Navigation
        public Guid MemberProfileId { get; set; }
        public virtual MemberProfile MemberProfile { get; set; } = null!;
        public virtual ICollection<WorkoutExercise> WorkoutExercises { get; set; } = [];
    }
    
}
