using ArenaDomain.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaDomain.Entities.Nutrition
{
    public class NutritionPlan : BaseEntity<Guid>
    {
        public Guid MemberProfileId { get; set; }

        public virtual MemberProfile MemberProfile { get; set; } = null!;

        public DateTime StartDate { get; set; }

        public decimal DailyCalories { get; set; }

        public decimal ProteinGrams { get; set; }

        public decimal CarbsGrams { get; set; }

        public decimal FatGrams { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation
        public virtual ICollection<Meal> Meals { get; set; } = [];
    }
}
