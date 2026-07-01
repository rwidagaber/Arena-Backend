using System;
using System.ComponentModel.DataAnnotations;

namespace ArenaApplication.Dtos.Workout
{
    public class MuscleGroupDto
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "The Name field is required.")]
        [Display(Name = "Muscle Group Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "The Arabic Name field is required.")]
        [Display(Name = "Arabic Muscle Group Name")]
        public string NameAr { get; set; } = string.Empty;
    }
}
