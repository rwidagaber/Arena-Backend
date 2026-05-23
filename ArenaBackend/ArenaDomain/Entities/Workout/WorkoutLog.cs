using ArenaDomain.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaDomain.Entities.Workout
{
    public class WorkoutLog : BaseEntity<Guid>
    {
        public Guid MemberProfileId { get; set; }

        public virtual MemberProfile MemberProfile { get; set; } = null!;

        public Guid? WorkoutPlanId { get; set; }

        public virtual WorkoutPlan? WorkoutPlan { get; set; }

        public DateTime WorkoutDate { get; set; }

        public string? Notes { get; set; }
    }
}
