using ArenaDomain.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaDomain.Entities.Workout
{
    public class WorkoutPlan : BaseEntity<Guid>
    {
        public Guid MemberProfileId { get; set; }

        public virtual MemberProfile MemberProfile { get; set; } = null!;

        public Guid? AssignedTrainerId { get; set; }


        public string Name { get; set; } = string.Empty;

        public int DurationWeeks { get; set; }

        public bool IsActive { get; set; } = true;

  

        // Navigation
        public virtual ICollection<WorkoutDay> WorkoutDays { get; set; } = [];

    }
}
