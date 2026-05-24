using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Dtos.WorkoutPlan
{
    public class WorkoutPlanDto
    {
        public Guid Id { get; set; }

        public Guid MemberProfileId { get; set; }

        public Guid? AssignedTrainerId { get; set; }

        public string Name { get; set; } = string.Empty;

        public int DurationWeeks { get; set; }

        public bool IsActive { get; set; }
    }
}
