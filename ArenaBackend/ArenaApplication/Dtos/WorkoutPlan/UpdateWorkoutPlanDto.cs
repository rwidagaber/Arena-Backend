using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ArenaApplication.Dtos.WorkoutPlan
{
    public class UpdateWorkoutPlanDto
    {
        [Required]
        public Guid Id { get; set; }

        public Guid? AssignedTrainerId { get; set; }

        public string Name { get; set; } = string.Empty;

        public int DurationWeeks { get; set; }

        public bool IsActive { get; set; }
    }
}
