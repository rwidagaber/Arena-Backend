using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ArenaApplication.Dtos.WorkoutPlan
{
    public class CreateWorkoutPlanDto
    {
        [Required]
        public Guid MemberProfileId { get; set; }

        public Guid? AssignedTrainerId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int DurationWeeks { get; set; }
    }
}
