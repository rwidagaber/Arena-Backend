using ArenaDomain.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaDomain.Entities.Nutrition
{
    public class Meal : BaseEntity<Guid>
    {
        public Guid NutritionPlanId { get; set; }

        public virtual NutritionPlan NutritionPlan { get; set; } = null!;

        public string MealType { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public decimal Calories { get; set; }

        public decimal Protein { get; set; }

        public decimal Carbs { get; set; }

        public decimal Fat { get; set; }

        public string Ingredients { get; set; } = string.Empty;
    }
}
