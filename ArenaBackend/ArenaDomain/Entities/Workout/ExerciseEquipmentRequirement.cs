using ArenaDomain.Entities.Gym;
using ArenaDomain.Shared;
using System;

namespace ArenaDomain.Entities.Workout
{
    public class ExerciseEquipmentRequirement : BaseEntity<Guid>
    {
        public Guid ExerciseCatalogItemId { get; set; }
        public virtual ExerciseCatalogItem ExerciseCatalogItem { get; set; } = null!;

        public Guid EquipmentId { get; set; }
        public virtual Equipment Equipment { get; set; } = null!;
    }
}
