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
        private readonly IMemberHealthRAGService _healthRAG;

        public ChatService(
            IGeminiCompletionService gemini,
            IWorkoutAIService workoutAI,
            INutritionAIService nutritionAI,
            IBookingAIService bookingAI,
            AppDbContext context,
            IGenericRepository<Booking, Guid> bookingRepo,
            IRAGService ragService,
            ILogger<ChatService> logger,
            IHostEnvironment environment,
            IMemberHealthRAGService healthRAG)
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
            _healthRAG = healthRAG;
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

            try
            {
                await _healthRAG.ExtractAndSaveFromChatAsync(profile.Id, userMessage);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save health context for member profile {MemberProfileId}", profile.Id);
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

            // Fetch upcoming bookings for context
            var upcomingBookings = await _bookingRepo.FindAsync(b =>
                b.MemberProfileId == profile.Id &&
                b.BookingDate.Date >= DateTime.UtcNow.Date &&
                b.Status != BookingStatus.Cancelled);

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

            var bookingContinuationIntent = DetectBookingContinuationIntent(userMessage, history);
            bool isArabic = IsArabic(userMessage)
                || (bookingContinuationIntent != null && HistoryLooksArabic(history));
            var intent = bookingContinuationIntent ?? await DetectIntentAsync(userMessage, history);
            if (intent != null)
                intent.RawMessage = userMessage;
            var memberName = GetMemberFirstName(profile);

            string reply;
            try
            {
                // ✅ Step 5 — Route and get reply
                reply = await RouteIntent(profile, intent, userMessage, isArabic, memberName, history, upcomingBookings);
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
                Timestamp = DateTime.UtcNow,
                Intent = intent?.Intent ?? "chat",
                Action = intent?.Action,
                BookingChanged = intent?.Intent == "booking"
                    && (intent.Action == "create" || intent.Action == "cancel" || intent.Action == "reschedule")
                    && IsSuccessfulBookingReply(reply)
            };
        }

        private async Task<IntentResult> DetectIntentAsync(string userMessage, List<ChatMessageDto> history)
        {
            try
            {
                var localIntent = DetectSimpleIntent(userMessage);
                if (localIntent != null)
                    return localIntent;

                var intentJson = await _gemini.GetCompletionAsync(
                    PromptLoader.GetIntentDetectionPrompt(),
                    history,
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
            List<ChatMessageDto> history,
            IEnumerable<Booking> upcomingBookings)
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
                            : DateTime.UtcNow.AddHours(3).Date; // Use Egypt time (UTC+3) not server local

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

                        // View Bookings
                        if (intent.Action == "view")
                        {
                            if (!upcomingBookings.Any())
                                return isArabic ? "ماعندكش حجوزات قادمة." : "You have no upcoming bookings.";

                            var bookingsList = string.Join("\n", upcomingBookings.Select(b =>
                                $"- {b.BookingDate:dddd, MMMM dd} at {b.StartTime:hh\\:mm}"));

                            return isArabic
                                ? $"📅 حجوزاتك القادمة:\n{bookingsList}"
                                : $"📅 Your upcoming bookings:\n{bookingsList}";
                        }

                        // No time → suggest slots
                        if (intent.Time == null)
                        {
                            var allSlots = new[] {
                                               "11:00","12:00","13:00","14:00","15:00",
                                               "16:00","17:00","18:00","19:00","20:00" };

                            // Use Egypt time (UTC+3) throughout to avoid midnight date-boundary issues
                            var egyptNow = DateTime.UtcNow.AddHours(3);
                            var egyptToday = egyptNow.Date;

                            IEnumerable<string> slotsToShow = allSlots;
                            if (targetDate.Date == egyptToday)
                            {
                                var currentTime = egyptNow.TimeOfDay;
                                slotsToShow = allSlots.Where(s => TimeSpan.Parse(s) > currentTime);
                            }

                            if (!slotsToShow.Any())
                            {
                                return isArabic
                                    ? "للأسف الأوقات المتاحة للنهارده خلصت. تحب تحجز لبكرة؟"
                                    : "Sorry, there are no more available times today. Would you like to book for tomorrow?";
                            }

                            var slotCrowds = slotsToShow.Select(slot =>
                            {
                                var st = TimeSpan.Parse(slot);
                                var count = dayBookings.Count(b =>
                                    Math.Abs((b.StartTime - st).TotalHours) < 1);
                                var level = count switch { < 3 => "🟢", < 7 => "🟡", _ => "🔴" };
                                return $"  {slot} {level} ";
                            });

                            var dateLabel = targetDate.Date == egyptToday.AddDays(1)
                                ? (isArabic ? "بكرة" : "tomorrow")
                                : targetDate.Date == egyptToday
                                ? (isArabic ? "النهارده" : "today")
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
                        var healthContext = await _healthRAG.GetRelevantHealthContextAsync(profile.Id, userMessage);
                        var healthAwareUserMessage = BuildHealthAwareUserMessage(userMessage, healthContext);

                        var foodPrompt = PromptLoader.GetFoodAnalysisPrompt(
                            name: memberName,
                            goal: profile.Goal ?? "General Fitness",
                            healthConditions: CombineHealthConditions(profile.HealthConditions, healthContext),
                            dietaryRestrictions: profile.DietaryRestrictions ?? "None",
                            weight: (profile.Weight ?? 70).ToString(),
                            userMessage: healthAwareUserMessage);

                        return await _gemini.GetCompletionAsync(
                            foodPrompt,
                            history,
                            healthAwareUserMessage);
                    }
                default:
                    {
                        // ✅ RAG: Search for relevant knowledge
                        var relevantKnowledge = await _ragService.SearchAsync(userMessage, topK: 7);

                        // ✅ RAG: Also search member-specific data
                        var memberData = await ((SimpleRAGService)_ragService)
                            .SearchMemberDataAsync(profile.Id, userMessage);

                        var healthContext = await _healthRAG.GetRelevantHealthContextAsync(profile.Id, userMessage);

                        var userContext = UserContextBuilder.Build(profile, null, null, upcomingBookings.ToList());
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

                        if (!string.IsNullOrEmpty(healthContext))
                            ragEnhancedPrompt += $"""


        === MEMBER'S KNOWN HEALTH HISTORY (CRITICAL - MUST RESPECT) ===
        {healthContext}
        ===============================================================
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

            return null;
        }

        private static bool ContainsAny(string text, params string[] values) =>
            values.Any(text.Contains);

        private static bool IsSuccessfulBookingReply(string reply)
        {
            return ContainsAny(
                reply,
                "Booking confirmed",
                "Booking cancelled",
                "Booking rescheduled",
                "تم تأكيد الحجز",
                "تم إلغاء الحجز",
                "تم تغيير الحجز");
        }

        private static IntentResult? DetectBookingContinuationIntent(
            string userMessage,
            List<ChatMessageDto> history)
        {
            if (!TryParseBookingTime(userMessage, out var time))
                return null;

            var recentAssistantMessage = history
                .Where(m => m.Sender == "assistant")
                .Reverse()
                .Take(5)
                .Select(m => m.MessageText.ToLowerInvariant())
                .FirstOrDefault(text => ContainsAny(text,
                    "available times", "tell me your preferred time", "preferred time",
                    "الأوقات المتاحة", "قولي الوقت", "هحجزلك", "اختار وقت",
                    "date and time of the booking you want to cancel", "تاريخ ووقت الحجز اللي عايز تلغيه",
                    "booking date and the new time", "تاريخ الحجز والوقت الجديد"));

            if (recentAssistantMessage == null)
                return null;

            string action = "create";
            if (ContainsAny(recentAssistantMessage, "cancel", "تلغيه", "إلغاء"))
            {
                action = "cancel";
            }
            else if (ContainsAny(recentAssistantMessage, "new time", "الوقت الجديد", "تغيير"))
            {
                action = "reschedule";
            }

            var previousBookingMessage = history
                .Where(m => m.Sender == "user")
                .Reverse()
                .FirstOrDefault(m => LooksLikeBookingRequest(m.MessageText));

            DateTime? date = null;
            if (previousBookingMessage != null
                && TryParseBookingDate(previousBookingMessage.MessageText, out var parsedDate))
                date = parsedDate;
            else if (TryParseBookingDate(userMessage, out var userParsedDate))
                date = userParsedDate;
            else
                date = TryParseDateFromSlotReply(history);

            if (!date.HasValue)
                return null;

            return new IntentResult
            {
                Intent = "booking",
                Action = action,
                Date = date.Value.ToString("yyyy-MM-dd"),
                Time = time.ToString(@"hh\:mm")
            };
        }

        private static bool TryParseBookingTime(string text, out TimeSpan time)
        {
            time = default;

            if (string.IsNullOrWhiteSpace(text))
                return false;

            var normalized = text.Trim()
                .Replace("：", ":")
                .Replace("الساعة", "", StringComparison.OrdinalIgnoreCase)
                .Replace("ساعه", "", StringComparison.OrdinalIgnoreCase)
                .Trim();

            var match = Regex.Match(normalized, @"^(?<hour>\d{1,2})(?::(?<minute>\d{1,2}))?\s*(?<period>am|pm|ص|م)?$",
                RegexOptions.IgnoreCase);

            if (!match.Success)
                return false;

            var hour = int.Parse(match.Groups["hour"].Value);
            var minute = match.Groups["minute"].Success
                ? int.Parse(match.Groups["minute"].Value)
                : 0;

            if (minute is < 0 or > 59)
                return false;

            var period = match.Groups["period"].Value.ToLowerInvariant();
            if ((period is "pm" or "م") && hour < 12)
                hour += 12;
            else if ((period is "am" or "ص") && hour == 12)
                hour = 0;
            else if (string.IsNullOrEmpty(period) && hour is >= 1 and <= 9)
                hour += 12;

            if (hour is < 0 or > 23)
                return false;

            time = new TimeSpan(hour, minute, 0);
            return true;
        }

        private static bool LooksLikeBookingRequest(string text)
        {
            var normalized = text.ToLowerInvariant();
            return ContainsAny(
                normalized,
                "book",
                "booking",
                "reserve",
                "schedule",
                "session",
                "احجز",
                "حجز",
                "عايز اجي",
                "عايز أجي",
                "موعد",
                "اجي",
                "أجي");
        }

        private static bool TryParseBookingDate(string text, out DateTime date)
        {
            var normalized = text.ToLowerInvariant();
            // Use Egypt time (UTC+3) as "today" reference to avoid midnight boundary issues
            var today = DateTime.UtcNow.AddHours(3).Date;

            if (ContainsAny(normalized, "tomorrow", "بكرة", "بكره"))
            {
                date = today.AddDays(1);
                return true;
            }

            if (ContainsAny(normalized, "after tomorrow", "بعد بكرة", "بعد بكره"))
            {
                date = today.AddDays(2);
                return true;
            }

            if (ContainsAny(normalized, "today", "النهارده", "النهاردة", "اليوم"))
            {
                date = today;
                return true;
            }

            var weekday = DetectWeekday(normalized);
            if (weekday.HasValue)
            {
                date = NextOrSameWeekday(today, weekday.Value);
                return true;
            }

            if (DateTime.TryParse(text, out date))
                return date.Date >= today;

            date = default;
            return false;
        }

        private static DateTime? TryParseDateFromSlotReply(List<ChatMessageDto> history)
        {
            var lastSlotReply = history
                .Where(m => m.Sender == "assistant")
                .Reverse()
                .FirstOrDefault(m => ContainsAny(
                    m.MessageText.ToLowerInvariant(),
                    "available times",
                    "الأوقات المتاحة"));

            if (lastSlotReply == null)
                return null;

            // Use Egypt time (UTC+3) as "today" reference
            var egyptToday = DateTime.UtcNow.AddHours(3).Date;

            var slotReplyText = lastSlotReply.MessageText.ToLowerInvariant();
            var dateFromLabel = TryParseDateFromSlotLabel(slotReplyText, egyptToday);
            if (dateFromLabel.HasValue)
                return dateFromLabel.Value;
            if (ContainsAny(slotReplyText, "after tomorrow", "Ø¨Ø¹Ø¯ Ø¨ÙƒØ±Ø©", "Ø¨Ø¹Ø¯ Ø¨ÙƒØ±Ù‡"))
                return egyptToday.AddDays(2);

            if (ContainsAny(slotReplyText, "tomorrow", "Ø¨ÙƒØ±Ø©", "Ø¨ÙƒØ±Ù‡"))
                return egyptToday.AddDays(1);

            if (ContainsAny(slotReplyText, "today", "Ø§Ù„Ù†Ù‡Ø§Ø±Ø¯Ù‡", "Ø§Ù„Ù†Ù‡Ø§Ø±Ø¯Ø©", "Ø§Ù„ÙŠÙˆÙ…"))
                return egyptToday;

            var weekday = DetectWeekday(lastSlotReply.MessageText.ToLowerInvariant());
            if (weekday.HasValue)
                return NextOrSameWeekday(egyptToday, weekday.Value);

            if (ContainsAny(lastSlotReply.MessageText.ToLowerInvariant(), "today", "النهارده", "النهاردة", "اليوم"))
                return egyptToday;

            if (ContainsAny(lastSlotReply.MessageText.ToLowerInvariant(), "tomorrow", "بكرة", "بكره"))
                return egyptToday.AddDays(1);

            if (ContainsAny(lastSlotReply.MessageText.ToLowerInvariant(), "after tomorrow", "بعد بكرة", "بعد بكره"))
                return egyptToday.AddDays(2);

            return null;
        }

        private static DateTime? TryParseDateFromSlotLabel(string text, DateTime today)
        {
            var month = DetectMonth(text);
            if (!month.HasValue)
                return null;

            var dayMatches = Regex.Matches(text, @"\b(?<day>\d{1,2})\b");
            foreach (Match match in dayMatches)
            {
                if (!int.TryParse(match.Groups["day"].Value, out var day))
                    continue;

                try
                {
                    var date = new DateTime(today.Year, month.Value, day);
                    if (date.Date < today.Date.AddDays(-1))
                        date = date.AddYears(1);

                    return date.Date;
                }
                catch (ArgumentOutOfRangeException)
                {
                }
            }

            return null;
        }

        private static int? DetectMonth(string text)
        {
            if (ContainsAny(text, "january", "\u064a\u0646\u0627\u064a\u0631")) return 1;
            if (ContainsAny(text, "february", "\u0641\u0628\u0631\u0627\u064a\u0631")) return 2;
            if (ContainsAny(text, "march", "\u0645\u0627\u0631\u0633")) return 3;
            if (ContainsAny(text, "april", "\u0623\u0628\u0631\u064a\u0644", "\u0627\u0628\u0631\u064a\u0644")) return 4;
            if (ContainsAny(text, "may", "\u0645\u0627\u064a\u0648")) return 5;
            if (ContainsAny(text, "june", "\u064a\u0648\u0646\u064a\u0648")) return 6;
            if (ContainsAny(text, "july", "\u064a\u0648\u0644\u064a\u0648")) return 7;
            if (ContainsAny(text, "august", "\u0623\u063a\u0633\u0637\u0633", "\u0627\u063a\u0633\u0637\u0633")) return 8;
            if (ContainsAny(text, "september", "\u0633\u0628\u062a\u0645\u0628\u0631")) return 9;
            if (ContainsAny(text, "october", "\u0623\u0643\u062a\u0648\u0628\u0631", "\u0627\u0643\u062a\u0648\u0628\u0631")) return 10;
            if (ContainsAny(text, "november", "\u0646\u0648\u0641\u0645\u0628\u0631")) return 11;
            if (ContainsAny(text, "december", "\u062f\u064a\u0633\u0645\u0628\u0631")) return 12;

            return null;
        }

        private static DayOfWeek? DetectWeekday(string text)
        {
            if (ContainsAny(text, "monday", "الاثنين", "الإثنين", "الاتنين"))
                return DayOfWeek.Monday;
            if (ContainsAny(text, "tuesday", "الثلاثاء", "التلات", "التلاتاء"))
                return DayOfWeek.Tuesday;
            if (ContainsAny(text, "wednesday", "الأربعاء", "الاربعاء", "الأربع", "الاربع"))
                return DayOfWeek.Wednesday;
            if (ContainsAny(text, "thursday", "الخميس"))
                return DayOfWeek.Thursday;
            if (ContainsAny(text, "friday", "الجمعة", "الجمعه"))
                return DayOfWeek.Friday;
            if (ContainsAny(text, "saturday", "السبت"))
                return DayOfWeek.Saturday;
            if (ContainsAny(text, "sunday", "الأحد", "الاحد", "الحد"))
                return DayOfWeek.Sunday;

            return null;
        }

        private static DateTime NextOrSameWeekday(DateTime from, DayOfWeek day)
        {
            var daysUntil = ((int)day - (int)from.DayOfWeek + 7) % 7;
            return from.Date.AddDays(daysUntil);
        }

        private static bool HistoryLooksArabic(List<ChatMessageDto> history) =>
            history
                .Reverse<ChatMessageDto>()
                .Take(4)
                .Any(m => m.MessageText.Any(c => c >= 0x0600 && c <= 0x06FF));

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

        private static string CombineHealthConditions(string? profileHealthConditions, string healthContext)
        {
            if (string.IsNullOrWhiteSpace(healthContext))
                return string.IsNullOrWhiteSpace(profileHealthConditions) ? "None" : profileHealthConditions;

            if (string.IsNullOrWhiteSpace(profileHealthConditions))
                return healthContext;

            return $"{profileHealthConditions}\n{healthContext}";
        }

        private static string BuildHealthAwareUserMessage(string userMessage, string healthContext)
        {
            if (string.IsNullOrWhiteSpace(healthContext))
                return userMessage;

            return $"""
            {userMessage}

            === MEMBER'S KNOWN HEALTH HISTORY (CRITICAL - MUST RESPECT) ===
            {healthContext}
            """;
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
