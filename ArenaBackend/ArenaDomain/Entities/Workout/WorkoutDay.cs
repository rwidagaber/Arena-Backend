using ArenaDomain.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaDomain.Entities.Workout
{
    public class WorkoutDay : BaseEntity<Guid>
    {
        public Guid WorkoutPlanId { get; set; }

        public virtual WorkoutPlan WorkoutPlan { get; set; } = null!;

        public int DayNumber { get; set; }

        public string DayName { get; set; } = string.Empty;

        // Navigation
        public virtual ICollection<WorkoutExercise> Exercises { get; set; }
    }
}