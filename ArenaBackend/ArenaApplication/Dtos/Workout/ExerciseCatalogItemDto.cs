using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ArenaApplication.Dtos.Workout
{
    public class ExerciseCatalogItemDto
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "The Name field is required.")]
        [Display(Name = "Exercise Name")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Arabic Exercise Name")]
        public string? NameAr { get; set; }

        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Arabic Description")]
        public string? DescriptionAr { get; set; }

        [Required(ErrorMessage = "The Muscle Group field is required.")]
        [Display(Name = "Muscle Group")]
        public string MuscleGroup { get; set; } = string.Empty;

        [Display(Name = "Arabic Muscle Group")]
        public string? MuscleGroupAr { get; set; }

        [Required(ErrorMessage = "The Difficulty Level field is required.")]
        [Display(Name = "Difficulty Level")]
        public string DifficultyLevel { get; set; } = "Beginner";

        [Display(Name = "Required Equipment")]
        public List<Guid> EquipmentIds { get; set; } = new List<Guid>();

        // For display purposes in the list
        public string EquipmentNames { get; set; } = string.Empty;
    }
}
