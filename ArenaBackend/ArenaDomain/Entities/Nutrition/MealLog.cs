using ArenaDomain.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaDomain.Entities.Nutrition
{
    public class MealLog : BaseEntity<Guid>
    {
        public Guid MemberProfileId { get; set; }

        public virtual MemberProfile MemberProfile { get; set; } = null!;

        public string ImageUrl { get; set; } = string.Empty;

        public decimal Calories { get; set; }

        public decimal Protein { get; set; }

        public decimal Carbs { get; set; }

        public decimal Fat { get; set; }

        public string FoodItems { get; set; } = string.Empty;

        public string? AIComment { get; set; }

        public DateTime LogDate { get; set; }
    }
}
