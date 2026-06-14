using ArenaApplication.AI;
using ArenaApplication.AI.ArenaApplication.AI;
using ArenaApplication.Dtos.ChatDtos;
using ArenaApplication.IServices;
using ArenaDomain.Entities.Bookings;
using ArenaDomain.Entities.Chat;
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
            IGenericRepository<Booking, Guid> bookingRepo)
        {
            _openAI = openAI;
            _workoutAI = workoutAI;
            _nutritionAI = nutritionAI;
            _bookingAI = bookingAI;
            _context = context;
            _bookingRepo = bookingRepo;
        }

        public async Task<ChatResponseWithHistoryDto> SendMessageAsync(
            Guid memberProfileId,
            Guid? conversationId,
            string userMessage)
        {
            var profile = await _context.MemberProfiles
                .FirstOrDefaultAsync(p => p.Id == memberProfileId
                                       || p.UserId == memberProfileId);

            if (profile == null)
                return new ChatResponseWithHistoryDto
                {
                    Reply = "❌ Profile not found.",
                    ConversationId = Guid.Empty
                };

            // ✅ Step 1 — Get or Create conversation
            ChatConversation conversation;

            if (conversationId.HasValue)
            {
                conversation = await _context.ChatConversations
                    .FirstOrDefaultAsync(c => c.Id == conversationId.Value
                                           && c.MemberProfileId == profile.Id)
                    ?? await CreateNewConversation(profile.Id, userMessage);
            }
            else
            {
                conversation = await CreateNewConversation(profile.Id, userMessage);
            }

            // ✅ Step 2 — Load last 10 messages for context
            var history = await _context.ChatMessages
                .Where(m => m.ChatConversationId == conversation.Id)
                .OrderByDescending(m => m.SentAt)
                .Take(10)
                .OrderBy(m => m.SentAt)
                .Select(m => new ChatMessageDto
                {
                    Sender = m.Sender == SenderType.User ? "user" : "assistant",
                    MessageText = m.MessageText
                })
                .ToListAsync();

            // ✅ Step 3 — Save user message
            _context.ChatMessages.Add(new ChatMessage
            {
                ChatConversationId = conversation.Id,
                MessageText = userMessage,
                Sender = SenderType.User,
                Intent = "pending",
                SentAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            // ✅ Step 4 — Detect intent
            var intentJson = await _openAI.GetCompletionAsync(
                PromptLoader.GetIntentDetectionPrompt(),
                new List<ChatMessageDto>(),
                userMessage);

            var cleanIntentJson = AIHelper.CleanJson(intentJson);
            var intent = JsonSerializer.Deserialize<IntentResult>(
                cleanIntentJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            bool isArabic = IsArabic(userMessage);

            // ✅ Step 5 — Route and get reply
            string reply = await RouteIntent(profile, intent, userMessage, isArabic, history);

            // ✅ Step 6 — Save AI reply
            _context.ChatMessages.Add(new ChatMessage
            {
                ChatConversationId = conversation.Id,
                MessageText = reply,
                Sender = SenderType.AI,
                Intent = intent?.Intent ?? "chat",
                SentAt = DateTime.UtcNow
            });

            // ✅ Step 7 — Update conversation title if new
            if (conversation.Title == "New Chat")
            {
                conversation.Title = GenerateTitle(userMessage);
                _context.ChatConversations.Update(conversation);
            }

            await _context.SaveChangesAsync();

            return new ChatResponseWithHistoryDto
            {
                ConversationId = conversation.Id,
                Reply = reply,
                Timestamp = DateTime.UtcNow
            };
        }

        private async Task<string> RouteIntent(
            ArenaDomain.Entities.MemberProfile profile,
            IntentResult? intent,
            string userMessage,
            bool isArabic,
            List<ChatMessageDto> history)
        {
            switch (intent?.Intent)
            {
                case "workout":
                    {
                        var workoutPlan = await _workoutAI
                            .GenerateWorkoutPlanAsync(profile.Id, userMessage);

                        var sb = new StringBuilder();
                        sb.AppendLine(isArabic
                            ? $"✅ تم إنشاء خطة التمرين '{workoutPlan.Name}'!"
                            : $"✅ Your workout plan '{workoutPlan.Name}' has been generated!");
                        sb.AppendLine(isArabic
                            ? $"📅 المدة: {workoutPlan.DurationWeeks} أسابيع\n"
                            : $"📅 Duration: {workoutPlan.DurationWeeks} weeks\n");

                        foreach (var day in workoutPlan.Days)
                        {
                            sb.AppendLine($"🏋️ {day.DayName}:");
                            foreach (var ex in day.Exercises)
                            {
                                if (ex.Name.ToLower().Contains("rest"))
                                    sb.AppendLine($"   • {ex.Name} 😴");
                                else if (ex.Sets <= 1 && ex.Reps >= 20)
                                    sb.AppendLine(isArabic
                                        ? $"   • {ex.Name} — {ex.Reps} دقيقة"
                                        : $"   • {ex.Name} — {ex.Reps} minutes");
                                else
                                    sb.AppendLine(isArabic
                                        ? $"   • {ex.Name} — {ex.Sets} مجموعات × {ex.Reps}"
                                        : $"   • {ex.Name} — {ex.Sets} sets x {ex.Reps} reps");
                            }
                            sb.AppendLine();
                        }

                        sb.AppendLine(isArabic
                            ? "💡 هل تريد خطة تغذية أيضاً؟"
                            : "💡 Want a nutrition plan too? Just ask!");

                        return sb.ToString();
                    }

                case "nutrition":
                    {
                        var nutritionPlan = await _nutritionAI
                            .GenerateNutritionPlanAsync(profile.Id, userMessage);

                        var nb = new StringBuilder();
                        nb.AppendLine(isArabic
                            ? $"✅ تم إعداد خطة التغذية يا {profile.FirstName}!"
                            : $"✅ Your nutrition plan is ready, {profile.FirstName}!");
                        nb.AppendLine(isArabic
                            ? $"🔥 السعرات: {nutritionPlan.DailyCalories} | 💪 بروتين: {nutritionPlan.ProteinGrams}g | 🍚 كارب: {nutritionPlan.CarbsGrams}g | 🥑 دهون: {nutritionPlan.FatGrams}g\n"
                            : $"🔥 Calories: {nutritionPlan.DailyCalories} | 💪 Protein: {nutritionPlan.ProteinGrams}g | 🍚 Carbs: {nutritionPlan.CarbsGrams}g | 🥑 Fat: {nutritionPlan.FatGrams}g\n");

                        foreach (var meal in nutritionPlan.Meals)
                        {
                            nb.AppendLine($"🍽️ {meal.MealType} — {meal.Name}");
                            nb.AppendLine(isArabic
                                ? $"   {meal.Calories} سعر | بروتين: {meal.ProteinGrams}g | كارب: {meal.CarbsGrams}g"
                                : $"   {meal.Calories} kcal | P: {meal.ProteinGrams}g | C: {meal.CarbsGrams}g | F: {meal.FatGrams}g");
                            nb.AppendLine($"   {meal.Ingredients}\n");
                        }

                        return nb.ToString();
                    }

                case "booking":
                    {
                        var targetDate = intent.Date != null
                            ? DateTime.Parse(intent.Date)
                            : DateTime.Now.AddDays(1);

                        var targetTime = intent.Time != null
                            ? TimeSpan.Parse(intent.Time)
                            : TimeSpan.Zero;

                        var dayBookings = _bookingRepo.GetAll()
                            .Where(b => b.BookingDate.Date == targetDate.Date)
                            .ToList();

                        // Cancel/Reschedule → skip crowd
                        if (intent.Action == "cancel" || intent.Action == "reschedule")
                            return await _bookingAI.HandleBookingRequestAsync(
                                profile.Id, intent, userMessage, profile.FirstName ?? "Member");

                        // No time → suggest slots
                        if (intent.Time == null)
                        {
                            var allSlots = new[] { "06:00","07:00","08:00","09:00","10:00",
                                               "11:00","12:00","13:00","14:00","15:00",
                                               "16:00","17:00","18:00","19:00","20:00" };

                            var slotCrowds = allSlots.Select(slot =>
                            {
                                var st = TimeSpan.Parse(slot);
                                var count = dayBookings.Count(b =>
                                    Math.Abs((b.StartTime - st).TotalHours) < 1);
                                var level = count switch { < 3 => "🟢", < 7 => "🟡", _ => "🔴" };
                                return $"  {slot} {level} ({count})";
                            });

                            var dateLabel = targetDate.Date == DateTime.Today.AddDays(1)
                                ? (isArabic ? "بكرة" : "tomorrow")
                                : targetDate.ToString("dddd, MMMM dd");

                            return isArabic
                                ? $"📅 الأوقات المتاحة {dateLabel}:\n{string.Join("\n", slotCrowds)}\n\nقولي الوقت وهحجزلك! 💪"
                                : $"📅 Available times for {dateLabel}:\n{string.Join("\n", slotCrowds)}\n\nTell me your preferred time! 💪";
                        }

                        // Has time → crowd + book
                        var same = dayBookings.Count(b =>
                            Math.Abs((b.StartTime - targetTime).TotalHours) < 1);

                        var crowd = same switch
                        {
                            < 3 => isArabic ? "🟢 الجيم هيكون هادي." : "🟢 The gym will be quiet.",
                            < 7 => isArabic ? "🟡 في ناس شوية." : "🟡 Moderate crowd expected.",
                            _ => isArabic ? "🔴 الجيم هيكون زحمة." : "🔴 The gym will be busy."
                        };

                        var bookingReply = await _bookingAI.HandleBookingRequestAsync(
                            profile.Id, intent, userMessage, profile.FirstName ?? "Member");

                        return $"{crowd}\n\n{bookingReply}";
                    }

                case "food_analysis":
                    {
                        var foodPrompt = PromptLoader.GetFoodAnalysisPrompt(
                            name: string.IsNullOrEmpty(profile.FirstName) ? "there" : profile.FirstName,
                            goal: profile.Goal ?? "General Fitness",
                            healthConditions: profile.HealthConditions ?? "None",
                            dietaryRestrictions: profile.DietaryRestrictions ?? "None",
                            weight: (profile.Weight ?? 70).ToString(),
                            userMessage: userMessage);

                        return await _openAI.GetCompletionAsync(
                            foodPrompt,
                            history,
                            userMessage);
                    }

                default:
                    {
                        // ✅ Pass history to AI for context
                        var userContext = UserContextBuilder.Build(profile);
                        var systemPrompt = PromptLoader.GetChatSystemPrompt(
                            userContext, profile.FirstName ?? "User");

                        return await _openAI.GetCompletionAsync(
                            systemPrompt, history, userMessage);
                    }
            }
        }

        // ✅ Helper Methods
        private async Task<ChatConversation> CreateNewConversation(
            Guid memberProfileId, string firstMessage)
        {
            var conversation = new ChatConversation
            {
                MemberProfileId = memberProfileId,
                Title = GenerateTitle(firstMessage),
                StartedAt = DateTime.UtcNow
            };
            _context.ChatConversations.Add(conversation);
            await _context.SaveChangesAsync();
            return conversation;
        }

        private static string GenerateTitle(string message)
        {
            if (string.IsNullOrEmpty(message)) return "New Chat";
            return message.Length > 40
                ? message.Substring(0, 40) + "..."
                : message;
        }

        private bool IsArabic(string text) =>
            text.Any(c => c >= 0x0600 && c <= 0x06FF);

        // ✅ Get all conversations for member
        public async Task<List<ConversationDto>> GetConversationsAsync(Guid memberProfileId)
        {
            var profile = await _context.MemberProfiles
                .FirstOrDefaultAsync(p => p.Id == memberProfileId
                                       || p.UserId == memberProfileId);

            if (profile == null) return [];

            return await _context.ChatConversations
                .Where(c => c.MemberProfileId == profile.Id)
                .OrderByDescending(c => c.StartedAt)
                .Select(c => new ConversationDto
                {
                    Id = c.Id,
                    Title = c.Title,
                    StartedAt = c.StartedAt,
                    MessageCount = c.Messages.Count,
                    LastMessage = c.Messages
                        .OrderByDescending(m => m.SentAt)
                        .Select(m => m.MessageText)
                        .FirstOrDefault() ?? ""
                })
                .ToListAsync();
        }

        // ✅ Get messages in a conversation
        public async Task<List<ChatResponseDto>> GetConversationMessagesAsync(
            Guid conversationId)
        {
            return await _context.ChatMessages
                .Where(m => m.ChatConversationId == conversationId)
                .OrderBy(m => m.SentAt)
                .Select(m => new ChatResponseDto
                {
                    Id = m.Id,
                    MessageText = m.MessageText,
                    Sender = m.Sender.ToString(),
                    Intent = m.Intent,
                    SentAt = m.SentAt
                })
                .ToListAsync();
        }

        // ✅ Create new conversation
        public async Task<ConversationDto> CreateConversationAsync(CreateConversationDto dto)
        {
            var profile = await _context.MemberProfiles
                .FirstOrDefaultAsync(p => p.Id == dto.MemberProfileId
                                       || p.UserId == dto.MemberProfileId);

            if (profile == null) throw new Exception("Profile not found");

            var conversation = new ChatConversation
            {
                MemberProfileId = profile.Id,
                Title = dto.Title,
                StartedAt = DateTime.UtcNow
            };

            _context.ChatConversations.Add(conversation);
            await _context.SaveChangesAsync();

            return new ConversationDto
            {
                Id = conversation.Id,
                Title = conversation.Title,
                StartedAt = conversation.StartedAt,
                MessageCount = 0,
                LastMessage = ""
            };
        }

        // ✅ Delete conversation
        public async Task DeleteConversationAsync(Guid conversationId)
        {
            var conversation = await _context.ChatConversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (conversation != null)
            {
                _context.ChatMessages.RemoveRange(conversation.Messages);
                _context.ChatConversations.Remove(conversation);
                await _context.SaveChangesAsync();
            }
        }
    }
}