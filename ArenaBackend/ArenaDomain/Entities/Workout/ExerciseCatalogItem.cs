using ArenaDomain.Shared;
using System;
using System.Collections.Generic;

namespace ArenaDomain.Entities.Workout
{
    public class ExerciseCatalogItem : BaseEntity<Guid>
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string MuscleGroup { get; set; } = string.Empty;
        public string DifficultyLevel { get; set; } = "Beginner"; // Beginner, Intermediate, Advanced

        // Navigation
        public virtual ICollection<ExerciseEquipmentRequirement> EquipmentRequirements { get; set; } = new List<ExerciseEquipmentRequirement>();
    }
}
