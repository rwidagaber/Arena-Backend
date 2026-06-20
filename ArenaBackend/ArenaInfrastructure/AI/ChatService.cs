using ArenaApplication.AI;
using ArenaApplication.AI.ArenaApplication.AI;
using ArenaApplication.Dtos.ChatDtos;
using ArenaApplication.IServices;
using ArenaDomain.Entities.Bookings;
using ArenaDomain.Entities.Chat;
using ArenaDomain.Enums;
using ArenaDomain.Interfaces;
using ArenaInfrastructure.AI;
using ArenaInfrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ArenaInfrastructure.AI
{
    public class ChatService : IChatService
    {
        private readonly IGeminiCompletionService _gemini;
        private readonly IWorkoutAIService _workoutAI;
        private readonly INutritionAIService _nutritionAI;
        private readonly IBookingAIService _bookingAI;
        private readonly AppDbContext _context;
        private readonly IGenericRepository<Booking, Guid> _bookingRepo;
        private readonly IRAGService _ragService;
        private readonly ILogger<ChatService> _logger;
        private readonly IHostEnvironment _environment;
        private const int MaxStoredMessageLength = 4000;
        private const int MaxTitleLength = 200;

        public ChatService(
            IGeminiCompletionService gemini,
            IWorkoutAIService workoutAI,
            INutritionAIService nutritionAI,
            IBookingAIService bookingAI,
            AppDbContext context,
            IGenericRepository<Booking, Guid> bookingRepo,
            IRAGService ragService,
            ILogger<ChatService> logger,
            IHostEnvironment environment)
        {
            _gemini = gemini;
            _workoutAI = workoutAI;
            _nutritionAI = nutritionAI;
            _bookingAI = bookingAI;
            _context = context;
            _bookingRepo = bookingRepo;
            _ragService = ragService;
            _logger = logger;
            _environment = environment;
        }

        public async Task<ChatResponseWithHistoryDto> SendMessageAsync(
            Guid memberProfileId,
            Guid? conversationId,
            string userMessage)
        {
            userMessage = userMessage?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(userMessage))
                return new ChatResponseWithHistoryDto
                {
                    Reply = "Please enter a message.",
                    ConversationId = conversationId ?? Guid.Empty
                };

            var profile = await _context.MemberProfiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == memberProfileId
                                       || p.UserId == memberProfileId);

            if (profile == null)
                return new ChatResponseWithHistoryDto
                {
                    Reply = "❌ Profile not found.",
                    ConversationId = Guid.Empty
                };

            // Check if the user has an active subscription that includes AI features
            var activeSub = await _context.UserSubscriptions
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.MemberProfileId == profile.Id 
                                       && s.Status == SubscriptionStatus.Active);

            if (activeSub == null || !activeSub.Plan.HasAI)
            {
                return new ChatResponseWithHistoryDto
                {
                    Reply = "❌ Access Denied: You need an active subscription with AI features enabled to use the AI Coach chatbot. Please upgrade your plan in the Pricing section.",
                    ConversationId = Guid.Empty
                };
            }

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
                MessageText = TruncateForStorage(userMessage),
                Sender = SenderType.User,
                Intent = "pending",
                SentAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            bool isArabic = IsArabic(userMessage);
            var intent = await DetectIntentAsync(userMessage);
            var memberName = GetMemberFirstName(profile);

            string reply;
            try
            {
                // ✅ Step 5 — Route and get reply
                reply = await RouteIntent(profile, intent, userMessage, isArabic, memberName, history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chat AI failed for member profile {MemberProfileId}", profile.Id);
                reply = BuildAssistantUnavailableReply(isArabic, ex);
                intent ??= new IntentResult { Intent = "chat" };
            }

            reply = TruncateForStorage(reply);

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

        public async Task<VoiceChatResponseDto> SendVoiceMessageAsync(
            Guid memberProfileId,
            Guid? conversationId,
            Stream audio,
            string audioContentType)
        {
            // ✅ Step 1 — Read the uploaded clip and transcribe it with Gemini
            using var memory = new MemoryStream();
            await audio.CopyToAsync(memory);
            var audioBase64 = Convert.ToBase64String(memory.ToArray());

            var mimeType = string.IsNullOrWhiteSpace(audioContentType)
                ? "audio/webm"
                : audioContentType;

            string transcript;
            try
            {
                transcript = (await _gemini.TranscribeAudioAsync(mimeType, audioBase64))?.Trim() ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Voice transcription failed for member profile {MemberProfileId}", memberProfileId);
                return new VoiceChatResponseDto
                {
                    Transcript = string.Empty,
                    Reply = "❌ Sorry, I couldn't process your voice note. Please try again.",
                    ConversationId = conversationId ?? Guid.Empty
                };
            }

            if (string.IsNullOrWhiteSpace(transcript))
                return new VoiceChatResponseDto
                {
                    Transcript = string.Empty,
                    Reply = "❌ Sorry, I couldn't understand the voice note. Please try again.",
                    ConversationId = conversationId ?? Guid.Empty
                };

            // ✅ Step 2 — Feed the transcript into the existing text pipeline
            var result = await SendMessageAsync(memberProfileId, conversationId, transcript);

            return new VoiceChatResponseDto
            {
                Transcript = transcript,
                ConversationId = result.ConversationId,
                Reply = result.Reply,
                Timestamp = result.Timestamp
            };
        }

        private async Task<IntentResult> DetectIntentAsync(string userMessage)
        {
            try
            {
                var localIntent = DetectSimpleIntent(userMessage);
                if (localIntent != null)
                    return localIntent;

                var intentJson = await _gemini.GetCompletionAsync(
                    PromptLoader.GetIntentDetectionPrompt(),
                    new List<ChatMessageDto>(),
                    userMessage);

                var cleanIntentJson = AIHelper.CleanJson(intentJson);
                return JsonSerializer.Deserialize<IntentResult>(
                    cleanIntentJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new IntentResult { Intent = "chat" };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Intent detection failed. Falling back to normal chat.");
                return new IntentResult { Intent = "chat" };
            }
        }

        private async Task<string> RouteIntent(
            ArenaDomain.Entities.MemberProfile profile,
            IntentResult? intent,
            string userMessage,
            bool isArabic,
            string memberName,
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
                            ? $"✅ يا {memberName}، تم إنشاء خطة التمرين '{workoutPlan.Name}'!"
                            : $"✅ {memberName}, your workout plan '{workoutPlan.Name}' has been generated!");
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
                            ? $"✅ تم إعداد خطة التغذية يا {memberName}!"
                            : $"✅ Your nutrition plan is ready, {memberName}!");
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
                                profile.Id, intent, userMessage, memberName);

                        // No time → suggest slots
                        if (intent.Time == null)
                        {
                            var allSlots = new[] {
                                               "11:00","12:00","13:00","14:00","15:00",
                                               "16:00","17:00","18:00","19:00","20:00" };

                            var slotCrowds = allSlots.Select(slot =>
                            {
                                var st = TimeSpan.Parse(slot);
                                var count = dayBookings.Count(b =>
                                    Math.Abs((b.StartTime - st).TotalHours) < 1);
                                var level = count switch { < 3 => "🟢", < 7 => "🟡", _ => "🔴" };
                                return $"  {slot} {level} ";
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
                            profile.Id, intent, userMessage, memberName);

                        return $"{crowd}\n\n{bookingReply}";
                    }

                case "food_analysis":
                    {
                        var foodPrompt = PromptLoader.GetFoodAnalysisPrompt(
                            name: memberName,
                            goal: profile.Goal ?? "General Fitness",
                            healthConditions: profile.HealthConditions ?? "None",
                            dietaryRestrictions: profile.DietaryRestrictions ?? "None",
                            weight: (profile.Weight ?? 70).ToString(),
                            userMessage: userMessage);

                        return await _gemini.GetCompletionAsync(
                            foodPrompt,
                            history,
                            userMessage);
                    }
                default:
                    {
                        // ✅ RAG: Search for relevant knowledge
                        var relevantKnowledge = await _ragService.SearchAsync(userMessage, topK: 7);

                        // ✅ RAG: Also search member-specific data
                        var memberData = await ((SimpleRAGService)_ragService)
                            .SearchMemberDataAsync(profile.Id, userMessage);

                        var userContext = UserContextBuilder.Build(profile);
                        var systemPrompt = PromptLoader.GetChatSystemPrompt(
                            userContext,
                            memberName,
                            GetLanguageInstruction(isArabic, userMessage));

                        // ✅ Add RAG context to prompt
                        var ragEnhancedPrompt = systemPrompt;

                        if (!string.IsNullOrEmpty(relevantKnowledge))
                            ragEnhancedPrompt += $"""


        === RELEVANT FITNESS KNOWLEDGE ===
        Use this specific knowledge to answer accurately:

        {relevantKnowledge}
        ===================================
        """;

                        if (!string.IsNullOrEmpty(memberData))
                            ragEnhancedPrompt += $"""


        === THIS MEMBER'S HISTORY ===
        {memberData}
        ============================
        """;

                        return await _gemini.GetCompletionAsync(
                            ragEnhancedPrompt, history, userMessage);
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
            var title = message.Length > 40
                ? message.Substring(0, 40) + "..."
                : message;

            return title.Length > MaxTitleLength
                ? title.Substring(0, MaxTitleLength)
                : title;
        }

        private static string TruncateForStorage(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value.Length <= MaxStoredMessageLength
                ? value
                : value.Substring(0, MaxStoredMessageLength);
        }

        private bool IsArabic(string text) =>
            text.Any(c => c >= 0x0600 && c <= 0x06FF);

        private static IntentResult? DetectSimpleIntent(string userMessage)
        {
            var text = userMessage.ToLowerInvariant();

            if (ContainsAny(text, "workout", "exercise", "training plan", "تمرين", "تدريب"))
                return new IntentResult { Intent = "workout" };

            if (ContainsAny(text, "nutrition", "meal plan", "diet", "calories", "غذاء", "دايت", "سعرات"))
                return new IntentResult { Intent = "nutrition" };

            if (ContainsAny(text, "food analysis", "analyze food", "تحليل الاكل", "حلل الاكل"))
                return new IntentResult { Intent = "food_analysis" };

            if (!ContainsAny(text, "book", "booking", "reserve", "cancel", "reschedule", "احجز", "حجز", "الغاء", "إلغاء"))
                return new IntentResult { Intent = "chat" };

            return null;
        }

        private static bool ContainsAny(string text, params string[] values) =>
            values.Any(text.Contains);

        private string BuildAssistantUnavailableReply(bool isArabic, Exception ex)
        {
            if (_environment.IsDevelopment())
            {
                return isArabic
                    ? $"مش قادر أوصل لخدمة المساعد دلوقتي. سبب الخطأ: {ex.Message}"
                    : $"I could not reach the assistant service right now. Error: {ex.Message}";
            }

            return isArabic
                ? "مش قادر أوصل لخدمة المساعد دلوقتي. جرب تاني كمان لحظة."
                : "I could not reach the assistant service right now. Please try again in a moment.";
        }

        private static string GetMemberFirstName(ArenaDomain.Entities.MemberProfile profile)
        {
            if (!string.IsNullOrWhiteSpace(profile.User?.FirstName))
                return profile.User.FirstName.Trim();

            if (!string.IsNullOrWhiteSpace(profile.FirstName))
                return profile.FirstName.Trim();

            return "Member";
        }

        private static string GetLanguageInstruction(bool isArabic, string userMessage)
        {
            return isArabic
                ? "Target language: Arabic. Use natural Arabic matching the user's tone. Do not switch to English except for unavoidable exercise or nutrition terms."
                : "Target language: English. Use clear professional English. Do not switch to Arabic.";
        }

        // ✅ Get all conversations for member
        public async Task<List<ConversationDto>> GetConversationsAsync(Guid memberProfileId)
        {
            var profile = await _context.MemberProfiles
                .Include(p => p.User)
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
                .Include(p => p.User)
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
