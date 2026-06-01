using ArenaApplication.AI;
using ArenaApplication.AI.ArenaApplication.AI;
using ArenaApplication.Dtos.ChatDtos;
using ArenaApplication.IServices;
using ArenaInfrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace ArenaApplication.Services.AI
{
    public class ChatService : IChatService
    {
        private readonly IOpenAIService _openAI;
        private readonly IWorkoutAIService _workoutAI;
        private readonly INutritionAIService _nutritionAI;
        private readonly IBookingAIService _bookingAI;
        private readonly AppDbContext _context;

        public ChatService(
            IOpenAIService openAI,
            IWorkoutAIService workoutAI,
            INutritionAIService nutritionAI,
            IBookingAIService bookingAI, 
            AppDbContext context)
        {
            _openAI = openAI;
            _workoutAI = workoutAI;
            _nutritionAI = nutritionAI;
            _bookingAI = bookingAI; 
            _context = context;
        }
        public async Task<string> SendMessageAsync(Guid memberProfileId, string userMessage)
        {
            var profile = await _context.MemberProfiles
                .FirstOrDefaultAsync(p => p.Id == memberProfileId || p.UserId == memberProfileId);

            if (profile == null)
                return "❌ Profile not found. Please complete your profile first.";

            // =========================
            // 1. INTENT DETECTION
            // =========================
            var intentJson = await _openAI.GetCompletionAsync(
                PromptBuilder.BuildIntentDetectionPrompt(),
                new List<ChatMessageDto>(),
                userMessage);

            var cleanIntentJson = AIHelper.CleanJson(intentJson);

            var intent = JsonSerializer.Deserialize<IntentResult>(
                cleanIntentJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // =========================
            // 2. LANGUAGE DETECTION
            // =========================
            bool isArabic = IsArabic(userMessage);

            // =========================
            // 3. ROUTING
            // =========================
            switch (intent?.Intent)
            {
                case "workout":
                    {
                        var workoutPlan = await _workoutAI
                            .GenerateWorkoutPlanAsync(profile.Id, userMessage);

                        var sb = new StringBuilder();

                        if (isArabic)
                        {
                            sb.AppendLine($"✅ تم إنشاء خطة التمرين الخاصة بك '{workoutPlan.Name}'!");
                            sb.AppendLine($"📅 المدة: {workoutPlan.DurationWeeks} أسابيع\n");
                        }
                        else
                        {
                            sb.AppendLine($"✅ Your workout plan '{workoutPlan.Name}' has been generated!");
                            sb.AppendLine($"📅 Duration: {workoutPlan.DurationWeeks} weeks\n");
                        }

                        foreach (var day in workoutPlan.Days)
                        {
                            sb.AppendLine(isArabic ? $"🏋️ {day.DayName}:" : $"🏋️ {day.DayName}:");

                            foreach (var ex in day.Exercises)
                            {
                                if (ex.Name.ToLower().Contains("rest"))
                                {
                                    sb.AppendLine(isArabic
                                        ? $"   • {ex.Name} 😴"
                                        : $"   • {ex.Name} 😴");
                                }
                                else if (ex.Sets <= 1 && ex.Reps >= 20)
                                {
                                    sb.AppendLine(isArabic
                                        ? $"   • {ex.Name} — {ex.Reps} دقيقة"
                                        : $"   • {ex.Name} — {ex.Reps} minutes");
                                }
                                else
                                {
                                    sb.AppendLine(isArabic
                                        ? $"   • {ex.Name} — {ex.Sets} مجموعات × {ex.Reps}"
                                        : $"   • {ex.Name} — {ex.Sets} sets x {ex.Reps} reps");
                                }
                            }

                            sb.AppendLine();
                        }

                        sb.AppendLine(isArabic
                            ? "💡 هل تريد خطة تغذية أيضاً؟ قل: اعمل لي نظام غذائي"
                            : "💡 Want a nutrition plan too? Just say 'Generate a nutrition plan for me'");

                        return sb.ToString();
                    }

                case "nutrition":
                    {
                        var nutritionPlan = await _nutritionAI
                            .GenerateNutritionPlanAsync(profile.Id, userMessage);

                        var nb = new StringBuilder();

                        if (isArabic)
                        {
                            nb.AppendLine($"✅ تم إعداد خطة التغذية الخاصة بك، {profile.FirstName}!");
                            nb.AppendLine($"🔥 السعرات اليومية: {nutritionPlan.DailyCalories}");
                            nb.AppendLine($"💪 البروتين: {nutritionPlan.ProteinGrams}g");
                            nb.AppendLine($"🍚 الكربوهيدرات: {nutritionPlan.CarbsGrams}g");
                            nb.AppendLine($"🥑 الدهون: {nutritionPlan.FatGrams}g\n");
                        }
                        else
                        {
                            nb.AppendLine($"✅ Your nutrition plan is ready, {profile.FirstName}!");
                            nb.AppendLine($"🔥 Daily Calories: {nutritionPlan.DailyCalories} kcal");
                            nb.AppendLine($"💪 Protein: {nutritionPlan.ProteinGrams}g");
                            nb.AppendLine($"🍚 Carbs: {nutritionPlan.CarbsGrams}g");
                            nb.AppendLine($"🥑 Fat: {nutritionPlan.FatGrams}g\n");
                        }

                        foreach (var meal in nutritionPlan.Meals)
                        {
                            if (isArabic)
                            {
                                nb.AppendLine($"🍽️ {meal.MealType} — {meal.Name}");
                                nb.AppendLine($"   السعرات: {meal.Calories} | بروتين: {meal.ProteinGrams} | كارب: {meal.CarbsGrams} | دهون: {meal.FatGrams}");
                                nb.AppendLine($"   المكونات: {meal.Ingredients}");
                            }
                            else
                            {
                                nb.AppendLine($"🍽️ {meal.MealType} — {meal.Name}");
                                nb.AppendLine($"   Calories: {meal.Calories} kcal | P: {meal.ProteinGrams}g | C: {meal.CarbsGrams}g | F: {meal.FatGrams}g");
                                nb.AppendLine($"   Ingredients: {meal.Ingredients}");
                            }

                            nb.AppendLine();
                        }

                        nb.AppendLine(isArabic
                            ? "💡 تريد خطة تمرين أيضاً؟ قل: اعمل لي خطة تمرين"
                            : "💡 Want a workout plan too? Just say 'Generate a workout plan for me'");

                        return nb.ToString();
                    }

                //case "booking":
                //    {
                //        if (intent.Date == null || intent.Time == null)
                //        {
                //            return isArabic
                //                ? "أحتاج التاريخ والوقت للحجز. مثال: احجز غداً الساعة 6"
                //                : "Please provide date and time. Example: book tomorrow at 6 PM";
                //        }

                //        return isArabic
                //            ? $"✅ تم تأكيد الحجز يوم {intent.Date} الساعة {intent.Time}"
                //            : $"✅ Booking confirmed for {intent.Date} at {intent.Time}";
                //    }

                case "booking":
                    var bookingReply = await _bookingAI
                        .HandleBookingRequestAsync(profile.Id, intent, userMessage);
                    return bookingReply;

                default:
                    {
                        var systemPrompt = PromptBuilder.BuildChatSystemPrompt(profile);

                        var reply = await _openAI.GetCompletionAsync(
                            systemPrompt,
                            new List<ChatMessageDto>(),
                            userMessage);

                        return reply;
                    }
            }
        }

        private bool IsArabic(string text)
        {
            return text.Any(c => c >= 0x0600 && c <= 0x06FF);
        }

        public Task<List<ChatMessageDto>> GetHistoryAsync(Guid memberProfileId)
        {
            return Task.FromResult(new List<ChatMessageDto>());
        }
    }
}