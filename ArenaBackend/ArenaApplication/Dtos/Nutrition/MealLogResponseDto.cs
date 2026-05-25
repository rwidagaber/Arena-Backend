using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Dtos.Nutrition
{
    public class MealLogResponseDto
    {
        public Guid Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public decimal Calories { get; set; }
        public decimal Protein { get; set; }
        public decimal Carbs { get; set; }
        public decimal Fat { get; set; }
        public string FoodItems { get; set; } = string.Empty;
        public string AIComment { get; set; }
        public DateTime LogDate { get; set; }
    }
}
