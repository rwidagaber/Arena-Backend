using ArenaApplication.AI;
using ArenaApplication.AI.ArenaApplication.AI;
using ArenaApplication.Dtos.ChatDtos;
using ArenaApplication.IServices;
using ArenaDomain.Entities.Bookings;
using ArenaDomain.Enums;
using ArenaDomain.Interfaces;
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
        private readonly IGenericRepository<Booking, Guid> _bookingRepo;

        public ChatService(
            IOpenAIService openAI,
            IWorkoutAIService workoutAI,
            INutritionAIService nutritionAI,
            IBookingAIService bookingAI, 
            AppDbContext context,
            IGenericRepository<Booking,Guid>bookingRepo)
        {
            _openAI = openAI;
            _workoutAI = workoutAI;
            _nutritionAI = nutritionAI;
            _bookingAI = bookingAI; 
            _context = context;
            _bookingRepo = bookingRepo;
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
    PromptLoader.GetIntentDetectionPrompt(),
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


                case "booking":
                    var targetDate = intent.Date != null
                        ? DateTime.Parse(intent.Date)
                        : DateTime.Now.AddDays(1);

                    var targetTime = intent.Time != null
                        ? TimeSpan.Parse(intent.Time)
                        : TimeSpan.Zero;

                    var todayBookings = _bookingRepo.GetAll()
                        .Where(b => b.BookingDate.Date == targetDate.Date)
                        .ToList();

                   
                    if (intent.Action == "cancel" || intent.Action == "reschedule")
                    {
                        return await _bookingAI
                            .HandleBookingRequestAsync(profile.Id, intent, userMessage);
                    }

                    
                    if (intent.Time == null)
                    {
                        var allSlots = new[] { "06:00", "07:00", "08:00", "09:00", "10:00",
                               "11:00", "12:00", "13:00", "14:00", "15:00",
                               "16:00", "17:00", "18:00", "19:00", "20:00" };

                        var slotCrowds = allSlots.Select(slot =>
                        {
                            var slotTime = TimeSpan.Parse(slot);
                            var count = todayBookings
                                .Count(b => Math.Abs((b.StartTime - slotTime).TotalHours) < 1);
                            var level = count switch
                            {
                                < 3 => "🟢 Quiet",
                                < 7 => "🟡 Moderate",
                                _ => "🔴 Busy"
                            };
                            return $"  {slot} → {level} ({count} bookings)";
                        }).ToList();

                        var dateLabel = targetDate.Date == DateTime.Today.AddDays(1)
                            ? "tomorrow"
                            : targetDate.ToString("dddd, MMMM dd");

                        return isArabic
                            ? $"""
                📅 الأوقات المتاحة {dateLabel}:

                {string.Join("\n", slotCrowds)}

                قولي الوقت اللي يناسبك وهحجزلك فوراً! 💪
                """
                            : $"""
                📅 Available times for {dateLabel}:

                {string.Join("\n", slotCrowds)}

                Tell me your preferred time and I'll book it for you! 💪
                """;
                    }

                    // ✅ عنده date و time → اعرض crowd ثم اعمل booking
                    var sameTimeBookings = todayBookings
                        .Count(b => Math.Abs((b.StartTime - targetTime).TotalHours) < 1);

                    string crowdMessage;
                    if (sameTimeBookings < 3)
                        crowdMessage = isArabic
                            ? "🟢 الجيم هيكون هادي في الوقت ده."
                            : "🟢 The gym will be quiet at this time.";
                    else if (sameTimeBookings < 7)
                        crowdMessage = isArabic
                            ? "🟡 في ناس شوية بس مش زحمة."
                            : "🟡 Moderate crowd expected, should be fine.";
                    else
                    {
                        var quietSlots = new[] { "6:00", "7:00", "13:00", "14:00", "20:00" }
                            .Where(slot =>
                            {
                                var st = TimeSpan.Parse(slot);
                                return todayBookings
                                    .Count(b => Math.Abs((b.StartTime - st).TotalHours) < 1) < 3;
                            })
                            .Take(3)
                            .ToList();

                        crowdMessage = isArabic
                            ? $"🔴 الجيم هيكون زحمة الساعة {intent.Time}."
                            : $"🔴 The gym will be busy at {intent.Time}.";

                        if (quietSlots.Any())
                            crowdMessage += isArabic
                                ? $"\n💡 أوقات أهدى: {string.Join(", ", quietSlots)}"
                                : $"\n💡 Quieter times: {string.Join(", ", quietSlots)}";
                    }

                    var bookingReply = await _bookingAI
                        .HandleBookingRequestAsync(profile.Id, intent, userMessage, profile.FirstName ?? "Member");

                    return crowdMessage + "\n\n" + bookingReply;
                default:
                    {
                       
                        var userContext = UserContextBuilder.Build(profile);

                        // Intent detection
                        var intentPrompt = PromptLoader.GetIntentDetectionPrompt();

                        // Chat
                        var systemPrompt = PromptLoader.GetChatSystemPrompt(
                            userContext: userContext,
                            name: profile.FirstName ?? "User");


                        //var systemPrompt = PromptBuilder.BuildChatSystemPrompt(profile, userContext);

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