namespace ArenaApplication.AI
{
    public static class PromptLoader
    {
        private static readonly string _promptsPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Prompts");

     

        private static string Load(string fileName)
        {
            
            var baseDir = Directory.GetCurrentDirectory();

           
            var possiblePaths = new[]
            {
        Path.Combine(baseDir, "AI", "Prompts", fileName),
        Path.Combine(baseDir, "..", "ArenaApplication", "AI", "Prompts", fileName),
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Prompts", fileName)
    };

            foreach (var path in possiblePaths)
            {
                var fullPath = Path.GetFullPath(path);
                if (File.Exists(fullPath))
                    return File.ReadAllText(fullPath);
            }

            throw new FileNotFoundException(
                $"Prompt file not found. Searched in:\n{string.Join("\n", possiblePaths)}");
        }

        public static string GetWorkoutPrompt(
            string userContext,
            string name,
            string goal,
            string injuries,
            string healthConditions,
            string experience,
            string equipment,
            string userMessage)
        {
            return Load("workout_prompt.txt")
                .Replace("{userContext}", userContext)
                .Replace("{name}", name)
                .Replace("{goal}", goal)
                .Replace("{injuries}", injuries)
                .Replace("{healthConditions}", healthConditions)
                .Replace("{experience}", experience)
                .Replace("{equipment}", equipment)
                .Replace("{userMessage}", userMessage);
        }

        public static string GetNutritionPrompt(
            string userContext,
            string goal,
            string dietaryRestrictions,
            string healthConditions,
            string userMessage)
        {
            return Load("nutrition_prompt.txt")
                .Replace("{userContext}", userContext)
                .Replace("{goal}", goal)
                .Replace("{dietaryRestrictions}", dietaryRestrictions)
                .Replace("{healthConditions}", healthConditions)
                .Replace("{userMessage}", userMessage);
        }

        public static string GetBookingPrompt(
    string name,
    bool hasSubscription,
    int remainingSessions,
    string subscriptionExpiry)
        {
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            var tomorrow = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");
            var afterTomorrow = DateTime.Now.AddDays(2).ToString("yyyy-MM-dd");

            return Load("booking_agent_prompt.txt")
                .Replace("{name}", name)
                .Replace("{hasSubscription}", hasSubscription ? "Yes" : "No")
                .Replace("{remainingSessions}", remainingSessions.ToString())
                .Replace("{subscriptionExpiry}", subscriptionExpiry)
                .Replace("{today}", today)
                .Replace("{tomorrow}", tomorrow)
                .Replace("{afterTomorrow}", afterTomorrow);
        }

        public static string GetIntentDetectionPrompt()
        {
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            var tomorrow = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");
            var afterTomorrow = DateTime.Now.AddDays(2).ToString("yyyy-MM-dd");

            return Load("intent_detection_prompt.txt")
                .Replace("{today}", today)
                .Replace("{tomorrow}", tomorrow)
                .Replace("{afterTomorrow}", afterTomorrow);
        }

        public static string GetChatSystemPrompt(string userContext, string name)
        {
            return Load("chat_system_prompt.txt")
                .Replace("{userContext}", userContext)
                .Replace("{name}", name);
        }


        public static string GetFoodAnalysisPrompt(
    string name,
    string goal,
    string healthConditions,
    string dietaryRestrictions,
    string weight,
    string userMessage)
        {
            return Load("food_analysis_prompt.txt")
                .Replace("{name}", name)
                .Replace("{goal}", goal)
                .Replace("{healthConditions}", healthConditions)
                .Replace("{dietaryRestrictions}", dietaryRestrictions)
                .Replace("{weight}", weight)
                .Replace("{userMessage}", userMessage);
        }
    }
}