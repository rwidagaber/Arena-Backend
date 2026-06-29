using ArenaDomain.Shared;
using System;

namespace ArenaDomain.Entities.Workout
{
    public class MuscleGroup : BaseEntity<Guid>
    {
        public string Name { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
    }
}
