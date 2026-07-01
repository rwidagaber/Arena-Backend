namespace ArenaApplication.Dtos.Nutrition
{
    public class MacroPercentageDto
    {
        public decimal Grams { get; set; }
        public decimal Percentage { get; set; }
    }

    public class MealImageAnalysisDto
    {
        public string MealName { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public decimal EstimatedCalories { get; set; }
        public MacroPercentageDto Protein { get; set; } = new();
        public MacroPercentageDto Carbs { get; set; } = new();
        public MacroPercentageDto Fat { get; set; } = new();
        public decimal Confidence { get; set; }
        public List<string> DetectedFoods { get; set; } = [];
        public List<string> Notes { get; set; } = [];
    }
}
