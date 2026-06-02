namespace ArenaApplication.Dtos.Nutrition
{
    public class MealResponseDto
    {
        public Guid Id { get; set; }

        public string MealType { get; set; } = string.Empty;
        // breakfast / lunch / dinner / snack

        public string Name { get; set; } = string.Empty;

        public decimal Calories { get; set; }

        public decimal ProteinGrams { get; set; }

        public decimal CarbsGrams { get; set; }

        public decimal FatGrams { get; set; }

        public string Ingredients { get; set; } = string.Empty;
    }
}