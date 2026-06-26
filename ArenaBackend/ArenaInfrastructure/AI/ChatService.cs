using ArenaApplication.AI;
using ArenaApplication.AI.ArenaApplication.AI;
using ArenaApplication.Dtos.Attendance;
using ArenaApplication.Dtos.ChatDtos;
using ArenaApplication.Dtos.HealthIntelligence;
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
using System.Globalization;
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
        private readonly IMemberHealthRAGService _healthRAG;
        private readonly IAttendanceSuggestionService _attendanceSuggestion;
        private readonly IHealthIntelligenceService _healthIntelligence;
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
            IHostEnvironment environment,
            IMemberHealthRAGService healthRAG,
            IAttendanceSuggestionService attendanceSuggestion,
            IHealthIntelligenceService healthIntelligence)
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
            _attendanceSuggestion = attendanceSuggestion;
            _healthIntelligence = healthIntelligence;
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
            //var activeSub = await _context.UserSubscriptions
            //    .Include(s => s.Plan)
            //    .FirstOrDefaultAsync(s => s.MemberProfileId == profile.Id 
            //                           && s.Status == SubscriptionStatus.Active);

            //if (activeSub == null || !activeSub.Plan.HasAI)
            //{
            //    return new ChatResponseWithHistoryDto
            //    {
            //        Reply = "❌ Access Denied: You need an active subscription with AI features enabled to use the AI Coach chatbot. Please upgrade your plan in the Pricing section.",
            //        ConversationId = Guid.Empty
            //    };
            //}

            if (!IsProfileMemoryQuestion(userMessage))
            {
                try
                {
                    await _healthRAG.ExtractAndSaveFromChatAsync(profile.Id, userMessage);
                    
                    var extraction = await _healthIntelligence.ExtractHealthProfileAsync(userMessage);
                    
                    var currentProfile = new HealthProfileDto();
                    if (!string.IsNullOrWhiteSpace(profile.HealthProfileJson))
                    {
                        currentProfile = JsonSerializer.Deserialize<HealthProfileDto>(profile.HealthProfileJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new HealthProfileDto();
                    }

                    // Merge newly extracted items
                    currentProfile.Conditions.AddRange(extraction.Conditions);
                    currentProfile.Allergies.AddRange(extraction.Allergies);
                    currentProfile.Injuries.AddRange(extraction.Injuries);
                    currentProfile.Restrictions.AddRange(extraction.Restrictions);
                    currentProfile.Medications.AddRange(extraction.Medications);

                    // Distinct
                    currentProfile.Conditions = currentProfile.Conditions.Distinct().ToList();
                    currentProfile.Allergies = currentProfile.Allergies.Distinct().ToList();
                    currentProfile.Injuries = currentProfile.Injuries.Distinct().ToList();
                    currentProfile.Restrictions = currentProfile.Restrictions.Distinct().ToList();
                    currentProfile.Medications = currentProfile.Medications.Distinct().ToList();

                    profile.HealthProfileJson = JsonSerializer.Serialize(currentProfile);

                    // Sync to legacy fields for backward compatibility
                    if (currentProfile.Conditions.Any() || currentProfile.Allergies.Any() || currentProfile.Medications.Any())
                    {
                        var allHealth = currentProfile.Conditions.Concat(currentProfile.Allergies).Concat(currentProfile.Medications);
                        profile.HealthConditions = string.Join(", ", allHealth);
                    }
                    if (currentProfile.Injuries.Any())
                    {
                        profile.Injuries = string.Join(", ", currentProfile.Injuries);
                    }
                    if (currentProfile.Restrictions.Any())
                    {
                        profile.DietaryRestrictions = string.Join(", ", currentProfile.Restrictions);
                    }

                    _context.MemberProfiles.Update(profile);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to save health context for member profile {MemberProfileId}", profile.Id);
                }
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

            var upcomingBookings = await _bookingRepo.FindAsync(b =>
                b.MemberProfileId == profile.Id &&
                b.BookingDate.Date >= DateTime.UtcNow.AddHours(3).Date &&
                b.Status != BookingStatus.Cancelled &&
                b.Status != BookingStatus.Expired);

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
            if (IsSimpleGreeting(userMessage))
            {
                intent = new IntentResult { Intent = "chat" };
            }
            if (intent != null)
            {
                intent.RawMessage = userMessage;
                intent.Intent = NormalizeIntent(intent.Intent, userMessage, history);

                bool profileUpdated = false;
                if (!string.IsNullOrWhiteSpace(intent.Goal)) { profile.Goal = intent.Goal; profileUpdated = true; }
                if (!string.IsNullOrWhiteSpace(intent.Injuries)) { profile.Injuries = intent.Injuries; profileUpdated = true; }
                if (!string.IsNullOrWhiteSpace(intent.HealthConditions)) { profile.HealthConditions = intent.HealthConditions; profileUpdated = true; }
                if (!string.IsNullOrWhiteSpace(intent.FitnessExperience)) { profile.FitnessExperience = intent.FitnessExperience; profileUpdated = true; }
                if (!string.IsNullOrWhiteSpace(intent.DietaryRestrictions)) { profile.DietaryRestrictions = intent.DietaryRestrictions; profileUpdated = true; }
                if (!string.IsNullOrWhiteSpace(intent.Equipment)) { profile.Equipment = intent.Equipment; profileUpdated = true; }
                
                if (!string.IsNullOrWhiteSpace(intent.WeightString)) 
                { 
                    var match = Regex.Match(intent.WeightString, @"\d+(\.\d+)?");
                    if (match.Success && decimal.TryParse(match.Value, out var w)) { profile.Weight = w; profileUpdated = true; }
                }
                if (!string.IsNullOrWhiteSpace(intent.HeightString)) 
                { 
                    var match = Regex.Match(intent.HeightString, @"\d+(\.\d+)?");
                    if (match.Success && decimal.TryParse(match.Value, out var h)) { profile.Height = h; profileUpdated = true; }
                }

                if (profileUpdated)
                {
                    _context.MemberProfiles.Update(profile);
                    await _context.SaveChangesAsync();
                }
            }
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

            // Extract Plan Data
            var planDataMatch = Regex.Match(reply, @"<PLAN_DATA>\s*({.*?})\s*</PLAN_DATA>", RegexOptions.Singleline);
            if (planDataMatch.Success)
            {
                var jsonStr = planDataMatch.Groups[1].Value;
                try
                {
                    using var doc = JsonDocument.Parse(jsonStr);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("targetCalories", out var cals) && cals.TryGetDecimal(out var dCals)) profile.TargetCalories = dCals;
                    if (root.TryGetProperty("targetProtein", out var prot) && prot.TryGetDecimal(out var dProt)) profile.TargetProtein = dProt;
                    if (root.TryGetProperty("targetCarbs", out var carbs) && carbs.TryGetDecimal(out var dCarbs)) profile.TargetCarbs = dCarbs;
                    if (root.TryGetProperty("targetFat", out var fat) && fat.TryGetDecimal(out var dFat)) profile.TargetFat = dFat;
                    if (root.TryGetProperty("framework", out var fw)) profile.CurrentPlanFramework = fw.GetString();
                    
                    _context.MemberProfiles.Update(profile);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse PLAN_DATA JSON.");
                }

                reply = reply.Replace(planDataMatch.Value, "").TrimEnd();
            }

            reply = TruncateForStorage(FormatUserVisibleReply(reply, intent?.Intent, isArabic));

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

        private async Task<IntentResult> DetectIntentAsync(string userMessage, List<ChatMessageDto> history)
        {
            try
            {
                if (IsProfileMemoryQuestion(userMessage))
                    return new IntentResult { Intent = "chat" };

                var intentJson = await _gemini.GetCompletionAsync(
                    PromptLoader.GetIntentDetectionPrompt(),
                    history,
                    userMessage);

                var cleanIntentJson = AIHelper.CleanJson(intentJson);
                var geminiResult = JsonSerializer.Deserialize<IntentResult>(
                    cleanIntentJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new IntentResult { Intent = "chat" };

                var clarificationIntent = DetectPlanClarificationIntent(userMessage, history);
                if (geminiResult.Intent == "chat" || geminiResult.Intent == null)
                {
                    if (clarificationIntent != null)
                    {
                        geminiResult.Intent = clarificationIntent.Intent;
                        geminiResult.PreferredDuration ??= clarificationIntent.PreferredDuration;
                        geminiResult.DietaryRestrictions ??= clarificationIntent.DietaryRestrictions;
                        geminiResult.Injuries ??= clarificationIntent.Injuries;
                        geminiResult.HealthConditions ??= clarificationIntent.HealthConditions;
                    }
                    else
                    {
                        var localIntent = DetectSimpleIntent(userMessage);
                        if (localIntent != null)
                        {
                            geminiResult.Intent = localIntent.Intent;
                            geminiResult.PreferredDuration ??= localIntent.PreferredDuration;
                            geminiResult.Equipment ??= localIntent.Equipment;
                        }
                    }
                }
                else
                {
                    if (clarificationIntent != null)
                    {
                        geminiResult.PreferredDuration ??= clarificationIntent.PreferredDuration;
                        geminiResult.DietaryRestrictions ??= clarificationIntent.DietaryRestrictions;
                        geminiResult.Injuries ??= clarificationIntent.Injuries;
                        geminiResult.HealthConditions ??= clarificationIntent.HealthConditions;
                    }
                }

                if (string.IsNullOrWhiteSpace(geminiResult.PreferredDuration))
                {
                    if (TryFindDuration(userMessage.Trim().ToLowerInvariant(), out var duration))
                    {
                        geminiResult.PreferredDuration = duration;
                    }
                    else
                    {
                        geminiResult.PreferredDuration = FindRecentDuration(history);
                    }
                }

                var recentAssistantMsg = history
                    .Where(m => m.Sender == "assistant")
                    .Reverse()
                    .FirstOrDefault()?.MessageText?.ToLowerInvariant();

                if (recentAssistantMsg != null)
                {
                    if (string.IsNullOrWhiteSpace(geminiResult.DietaryRestrictions) && 
                        ContainsAny(recentAssistantMsg, "dietary restrictions", "food allergies", "\u062d\u0633\u0627\u0633\u064a\u0629", "\u0646\u0638\u0627\u0645 \u063a\u0630\u0627\u0626\u064a"))
                    {
                        if (!string.IsNullOrWhiteSpace(geminiResult.HealthConditions) && geminiResult.HealthConditions != "None")
                            geminiResult.DietaryRestrictions = "None"; 
                        else if (geminiResult.Intent != "chat" && !string.IsNullOrWhiteSpace(userMessage))
                            geminiResult.DietaryRestrictions = userMessage;
                    }
                    
                    if (string.IsNullOrWhiteSpace(geminiResult.Injuries) && 
                        ContainsAny(recentAssistantMsg, "injuries", "\u0625\u0635\u0627\u0628\u0627\u062a", "\u0623\u0645\u0631\u0627\u0636"))
                    {
                        if (!string.IsNullOrWhiteSpace(geminiResult.HealthConditions) && geminiResult.HealthConditions != "None")
                            geminiResult.Injuries = "None";
                        else if (geminiResult.Intent != "chat" && !string.IsNullOrWhiteSpace(userMessage))
                            geminiResult.Injuries = userMessage;
                    }

                    if (string.IsNullOrWhiteSpace(geminiResult.HealthConditions) && 
                        ContainsAny(recentAssistantMsg, "health conditions", "\u0625\u0635\u0627\u0628\u0627\u062a", "\u0623\u0645\u0631\u0627\u0636"))
                    {
                        if (!string.IsNullOrWhiteSpace(geminiResult.Injuries) && geminiResult.Injuries != "None")
                            geminiResult.HealthConditions = "None";
                        else if (geminiResult.Intent != "chat" && !string.IsNullOrWhiteSpace(userMessage))
                            geminiResult.HealthConditions = userMessage;
                    }
                }

                return geminiResult;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Intent detection failed. Falling back to normal chat.");
                return new IntentResult { Intent = "chat" };
            }
        }

        private static DateTime ParseAttendanceDate(string? date)
        {
            if (!string.IsNullOrWhiteSpace(date))
            {
                // The intent prompt emits yyyy-MM-dd; parse culture-invariantly.
                if (DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact))
                    return exact.Date;
                if (DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                    return parsed.Date;
            }
            return DateTime.UtcNow.AddHours(3).Date; // local (Egypt) today
        }

        private static string FormatHour12Ar(int hour24)
        {
            var period = hour24 >= 12 ? "م" : "ص";
            var hour12 = hour24 % 12;
            if (hour12 == 0) hour12 = 12;
            return $"{hour12} {period}";
        }

        private static string FormatHour12En(int hour24)
        {
            var period = hour24 >= 12 ? "PM" : "AM";
            var hour12 = hour24 % 12;
            if (hour12 == 0) hour12 = 12;
            return $"{hour12} {period}";
        }

        // Bilingual chat reply: recommends the quietest time(s) and warns about the
        // busy / over-capacity (full) hours to avoid.
        private static string BuildAttendanceReply(AttendanceSuggestionDto suggestion, bool isArabic)
        {
            var sep = isArabic ? "، " : ", ";

            var quiet = string.Join(sep, suggestion.RecommendedHours.Take(3)
                .Select(h => isArabic ? FormatHour12Ar(h) : FormatHour12En(h)));

            // Busiest / full = over-capacity (>5 bookings) or High crowd level.
            var avoid = string.Join(sep, suggestion.Occupancy.Slots
                .Where(slot => slot.OverCapacity || slot.Level == "High")
                .Take(4)
                .Select(slot => isArabic ? FormatHour12Ar(slot.Hour) : FormatHour12En(slot.Hour)));

            if (isArabic)
            {
                var reply = $"🟢 أهدأ وقت يوم {suggestion.DayOfWeek} تقريباً {quiet} — ده أنسب وقت تيجي فيه.";
                if (!string.IsNullOrEmpty(avoid))
                    reply += $"\n🔴 الأزحم/الممتلئ: {avoid} — حاول تتجنبها.";
                return reply;
            }

            var enReply = $"🟢 Best time on {suggestion.DayOfWeek}: around {quiet} (quietest) — that's your window.";
            if (!string.IsNullOrEmpty(avoid))
                enReply += $"\n🔴 Busiest/full: {avoid} — best to avoid these.";
            return enReply;
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
            if (intent?.Intent == "workout" || intent?.Intent == "nutrition" || intent?.Intent == "both")
            {
                // We now pull equipment, injuries, and experience directly from the system or use intelligent defaults.
                // We no longer block the user to interrogate them, fulfilling the requirement for a friendlier AI.
            }

            switch (intent?.Intent)
            {
                case "both":
                    {
                        var workoutPlan = await _workoutAI
                            .GenerateWorkoutPlanAsync(profile.Id, userMessage);
                        var nutritionPlan = await _nutritionAI
                            .GenerateNutritionPlanAsync(profile.Id, userMessage);

                        var combined = new StringBuilder();
                        combined.AppendLine(isArabic
                            ? $"✅ تم إعداد خطتي التمرين والتغذية بنجاح يا {memberName}! 🚀 يلا نبدأ نحقق أهدافك:"
                            : $"✅ All set, {memberName}! 🚀 I've put together your personalized workout and nutrition plans. Let's get to work:");
                        combined.AppendLine();

                        // --- WORKOUT SECTION ---
                        combined.AppendLine(isArabic ? "🏋️ **خطة التمرين الخاصة بك**" : "🏋️ **YOUR WORKOUT PLAN**");
                        combined.AppendLine($"🎯 {workoutPlan.Name} ({(isArabic ? $"{workoutPlan.DurationWeeks} أسابيع" : $"{workoutPlan.DurationWeeks} weeks")})");
                        foreach (var day in workoutPlan.Days)
                        {
                            var exerciseNames = string.Join(", ", day.Exercises.Take(5).Select(ex => ex.Name));
                            combined.AppendLine($"   • **{day.DayName}**: {exerciseNames}{(day.Exercises.Count > 5 ? "..." : "")}");
                        }
                        combined.AppendLine();

                        // --- NUTRITION SECTION ---
                        combined.AppendLine(isArabic ? "🥗 **خطة التغذية الخاصة بك**" : "🥗 **YOUR NUTRITION PLAN**");
                        combined.AppendLine(isArabic
                            ? $"🔥 هدفك اليومي: {nutritionPlan.DailyCalories} سعر حراري | بروتين {nutritionPlan.ProteinGrams}g | كارب {nutritionPlan.CarbsGrams}g | دهون {nutritionPlan.FatGrams}g"
                            : $"🔥 Daily Goal: {nutritionPlan.DailyCalories} kcal | Protein {nutritionPlan.ProteinGrams}g | Carbs {nutritionPlan.CarbsGrams}g | Fat {nutritionPlan.FatGrams}g");

                        foreach (var meal in nutritionPlan.Meals)
                        {
                            combined.AppendLine(isArabic
                                ? $"   • **{meal.MealType}**: {meal.Name} ({meal.Calories} سعر)"
                                : $"   • **{meal.MealType}**: {meal.Name} ({meal.Calories} kcal)");
                        }
                        combined.AppendLine();

                        combined.AppendLine(isArabic
                            ? "💡 تفاصيل التمارين والمكونات الغذائية محفوظة بالكامل في حسابك. تقدر ترجع لها في أي وقت!"
                            : "💡 Full exercise details, sets, reps, and meal ingredients are securely saved to your dashboard. You can access them anytime!");

                        return combined.ToString();
                    }
                case "workout":
                    {
                        var workoutPlan = await _workoutAI
                            .GenerateWorkoutPlanAsync(profile.Id, userMessage);

                        var sb = new StringBuilder();
                        sb.AppendLine(isArabic
                            ? $"✅ يا {memberName}، تم إنشاء خطة التمرين '{workoutPlan.Name}' بنجاح! جاهز للتحدي؟ 💪"
                            : $"✅ {memberName}, your custom workout plan '{workoutPlan.Name}' is ready to go! Let's crush those goals! 💪");
                        sb.AppendLine(isArabic
                            ? $"📅 المدة: {workoutPlan.DurationWeeks} أسابيع\n"
                            : $"📅 Plan Duration: {workoutPlan.DurationWeeks} weeks\n");

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
                            ? "💡 بالمناسبة، لو محتاج نظام غذائي يساعدك توصل لهدفك أسرع، أنا موجود في أي وقت!"
                            : "💡 By the way, if you need a nutrition plan to complement your workouts, just let me know!");

                        return sb.ToString();
                    }

                case "goal_change":
                    {
                        var newGoal = DetectGoalFromMessage(userMessage);
                        if (newGoal != null && !string.Equals(profile.Goal, newGoal, StringComparison.OrdinalIgnoreCase))
                        {
                            var oldGoal = profile.Goal ?? "Not set";
                            profile.Goal = newGoal;
                            _context.MemberProfiles.Update(profile);
                            await _context.SaveChangesAsync();
                        }
                        
                        // Generate a new workout plan aligned with the new goal
                        var workoutPlan = await _workoutAI.GenerateWorkoutPlanAsync(profile.Id, userMessage);

                        var sb = new StringBuilder();
                        sb.AppendLine(isArabic
                            ? $"✅ رائع يا {memberName}! لقد حدثنا هدفك إلى '{newGoal}'. وتم تصميم خطة التمرين الجديدة '{workoutPlan.Name}' خصيصاً لك! 🚀"
                            : $"✅ Awesome {memberName}! I've updated your goal to '{newGoal}' and crafted a brand new workout plan '{workoutPlan.Name}' just for you! 🚀");
                        sb.AppendLine(isArabic
                            ? $"📅 المدة: {workoutPlan.DurationWeeks} أسابيع\n"
                            : $"📅 Plan Duration: {workoutPlan.DurationWeeks} weeks\n");

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
                            ? "💡 لو حابب نظام غذائي يناسب هدفك الجديد، اطلب مني في أي وقت!"
                            : "💡 If you'd like a fresh nutrition plan to match your new goal, I'm ready whenever you are!");

                        return sb.ToString();
                    }

                case "nutrition":
                    {
                        var nutritionPlan = await _nutritionAI
                            .GenerateNutritionPlanAsync(profile.Id, userMessage);

                        var nb = new StringBuilder();
                        nb.AppendLine(isArabic
                            ? $"✅ تفضل يا {memberName}، لقد جهزت نظامك الغذائي المخصص! 🥗 أتمنى أن يعجبك:"
                            : $"✅ Here you go, {memberName}! I've put together a delicious, personalized nutrition plan for you! 🥗");
                        nb.AppendLine(isArabic
                            ? $"🔥 هدفك اليومي: {nutritionPlan.DailyCalories} سعر حراري | 💪 بروتين: {nutritionPlan.ProteinGrams}g | 🍚 كارب: {nutritionPlan.CarbsGrams}g | 🥑 دهون: {nutritionPlan.FatGrams}g\n"
                            : $"🔥 Daily Goal: {nutritionPlan.DailyCalories} kcal | 💪 Protein: {nutritionPlan.ProteinGrams}g | 🍚 Carbs: {nutritionPlan.CarbsGrams}g | 🥑 Fat: {nutritionPlan.FatGrams}g\n");

                        foreach (var meal in nutritionPlan.Meals)
                        {
                            nb.AppendLine($"🍽️ **{meal.MealType}** — {meal.Name}");
                            nb.AppendLine(isArabic
                                ? $"   {meal.Calories} سعر حراري | بروتين: {meal.ProteinGrams}g | كارب: {meal.CarbsGrams}g"
                                : $"   {meal.Calories} kcal | P: {meal.ProteinGrams}g | C: {meal.CarbsGrams}g | F: {meal.FatGrams}g");
                            nb.AppendLine($"   *{meal.Ingredients}*\n");
                        }

                        return nb.ToString();
                    }

                case "booking":
                    {
                        var targetDate = intent.Date != null
                            ? DateTime.Parse(intent.Date)
                            : DateTime.UtcNow.AddHours(3).Date;

                        var targetTime = intent.Time != null
                            ? TimeSpan.Parse(intent.Time)
                            : TimeSpan.Zero;

                        // Only confirmed bookings count toward crowd/traffic
                        // (cancelled, pending, expired, etc. are excluded).
                        var dayBookings = _bookingRepo.GetAll()
                            .Where(b => b.BookingDate.Date == targetDate.Date
                                && b.Status == BookingStatus.Confirmed)
                            .ToList();

                        // Cancel/Reschedule → skip crowd
                        if (intent.Action == "cancel" || intent.Action == "reschedule")
                            return await _bookingAI.HandleBookingRequestAsync(
                                profile.Id, intent, userMessage, memberName);

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

                        // No time → suggest slots built from the gym's DB working hours.
                        if (intent.Time == null)
                        {
                            var egyptNow = DateTime.UtcNow.AddHours(3);
                            var egyptToday = egyptNow.Date;

                            // Pull the day's open hours + crowd profile from the database.
                            var occupancyResult = await _attendanceSuggestion.GetDayOccupancyAsync(targetDate);
                            var occupancy = occupancyResult.Value;

                            if (occupancy == null || occupancy.IsClosed || occupancy.Slots.Count == 0)
                                return isArabic
                                    ? $"الجيم مقفول يوم {targetDate:dddd}. اختار يوم تاني."
                                    : $"The gym is closed on {targetDate:dddd}. Please pick another day.";

                            IEnumerable<OccupancySlotDto> slotsToShow = occupancy.Slots;
                            if (targetDate.Date == egyptToday)
                            {
                                var currentHour = egyptNow.Hour;
                                slotsToShow = occupancy.Slots.Where(s => s.Hour > currentHour);
                            }

                            if (!slotsToShow.Any())
                            {
                                return isArabic
                                    ? "للأسف الأوقات المتاحة للنهارده خلصت. تحب تحجز لبكرة؟"
                                    : "Sorry, there are no more available times today. Would you like to book for tomorrow?";
                            }

                            var slotCrowds = slotsToShow.Select(s =>
                            {
                                var level = s.OverCapacity || s.Level == "High" ? "🔴"
                                          : s.Level == "Medium" ? "🟡" : "🟢";
                                return $"  {s.Hour:00}:00 {level} ";
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

                        // 5+ confirmed bookings in the same slot = traffic/busy.
                        var crowd = same switch
                        {
                            < 3 => isArabic ? "🟢 الجيم هيكون هادي." : "🟢 The gym will be quiet.",
                            < 5 => isArabic ? "🟡 في ناس شوية." : "🟡 Moderate crowd expected.",
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

                        // If the user is asking a GENERAL food question (no specific foods listed),
                        // answer directly as a coach instead of asking them to list available foods.
                        if (!UserMentionsSpecificFoods(userMessage))
                        {
                            var userContext = await BuildFullConversationContextAsync(profile, history);
                            var systemPrompt = PromptLoader.GetChatSystemPrompt(
                                userContext,
                                memberName,
                                GetLanguageInstruction(isArabic, userMessage));

                            var relevantKnowledge = await _ragService.SearchAsync(userMessage, topK: 5);
                            if (!string.IsNullOrEmpty(relevantKnowledge))
                                systemPrompt += $"""


        === RELEVANT FITNESS KNOWLEDGE ===
        {relevantKnowledge}
        ==================================
        """;

                            if (!string.IsNullOrEmpty(healthContext))
                                systemPrompt += $"""


        === MEMBER'S KNOWN HEALTH HISTORY (CRITICAL - MUST RESPECT) ===
        {healthContext}
        ===============================================================
        """;

                            return await _gemini.GetCompletionAsync(systemPrompt, history, userMessage);
                        }

                        // User listed specific food items — use the food-analysis JSON pipeline.
                        var foodPrompt = PromptLoader.GetFoodAnalysisPrompt(
                            name: memberName,
                            goal: profile.Goal ?? "General Fitness",
                            healthConditions: CombineHealthConditions(profile.HealthConditions, healthContext),
                            dietaryRestrictions: profile.DietaryRestrictions ?? "None",
                            weight: (profile.Weight ?? 70).ToString(),
                            userMessage: healthAwareUserMessage);

                        var rawFoodReply = await _gemini.GetCompletionAsync(
                            foodPrompt,
                            history,
                            healthAwareUserMessage);

                        return FormatNutritionJsonReply(rawFoodReply, isArabic);
                    }
                case "ask_about_injury_compatibility":
                    {
                        var userContext = await BuildFullConversationContextAsync(profile, history);
                        var activeWorkout = await GetActiveWorkoutPlanContextAsync(profile.Id);
                        if (string.IsNullOrWhiteSpace(activeWorkout))
                        {
                            return isArabic
                                ? "\u0645\u062d\u062a\u0627\u062c \u062e\u0637\u0629 \u062a\u0645\u0631\u064a\u0646 \u0645\u062d\u0641\u0648\u0638\u0629 \u0623\u0648\u0644\u0627\u064b \u0639\u0634\u0627\u0646 \u0623\u0642\u064a\u0651\u0645\u0647\u0627 \u0639\u0644\u0649 \u0625\u0635\u0627\u0628\u062a\u0643. \u0644\u0648 \u0639\u0646\u062f\u0643 \u062e\u0637\u0629 \u0645\u0639\u064a\u0646\u0629\u060c \u0627\u0628\u0639\u062a\u0647\u0627 \u0644\u064a \u0648\u0647\u062d\u0644\u0644\u0647\u0627 \u062a\u0645\u0631\u064a\u0646 \u0628\u062a\u0645\u0631\u064a\u0646."
                                : "I need an active saved workout plan before I can assess it against your injury. Send me the plan if it is not saved here, and I will review it exercise by exercise.";
                        }

                        var prompt = $"""
                        You are Arena AI, a professional fitness coach.
                        Reply in Arabic only, with a friendly coaching tone.

                        Analyze the ACTIVE workout plan against the member's injuries and limitations.
                        Do NOT create a new workout plan.
                        For every exercise, classify it exactly as one of:
                        - \u2705 Safe
                        - \u26a0\ufe0f Use With Caution
                        - \u274c Not Recommended

                        Explain why, and give a safer alternative when caution or not recommended is used.
                        End with "\ud83d\udca1 Final Recommendation" and say whether the plan is suitable overall.

                        === FULL MEMBER CONTEXT ===
                        {userContext}

                        === ACTIVE WORKOUT PLAN TO ANALYZE ===
                        {activeWorkout}
                        """;

                        return await _gemini.GetCompletionAsync(prompt, history, userMessage);
                    }
                case "attendance":
                    {
                        var date = ParseAttendanceDate(intent?.Date);
                        var suggestionResult = await _attendanceSuggestion.SuggestBestTimeAsync(date);
                        if (!suggestionResult.IsSuccess || suggestionResult.Value == null)
                            return isArabic
                                ? "معلش، مش قادر أقترح وقت دلوقتي. جرّب تاني."
                                : "Sorry, I couldn't suggest a time right now. Please try again.";

                        var suggestion = suggestionResult.Value;
                        if (suggestion.IsClosed || suggestion.RecommendedHours.Count == 0)
                            return isArabic
                                ? $"الجيم مقفول يوم {suggestion.DayOfWeek}. جرّب يوم تاني."
                                : $"The gym is closed on {suggestion.DayOfWeek}. Try another day.";

                        return BuildAttendanceReply(suggestion, isArabic);
                    }
                default:
                    {
                        // ✅ RAG: Search for relevant knowledge
                        var relevantKnowledge = await _ragService.SearchAsync(userMessage, topK: 7);

                        // ✅ RAG: Also search member-specific data
                        var memberData = await ((SimpleRAGService)_ragService)
                            .SearchMemberDataAsync(profile.Id, userMessage);

                        var healthContext = await _healthRAG.GetRelevantHealthContextAsync(profile.Id, userMessage);

                        var userContext = await BuildFullConversationContextAsync(profile, history);
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
        private async Task<string> BuildFullConversationContextAsync(
            ArenaDomain.Entities.MemberProfile profile,
            List<ChatMessageDto> history)
        {
            var workoutPlans = await _context.WorkoutPlans
                .Include(p => p.WorkoutDays)
                    .ThenInclude(d => d.Exercises)
                        .ThenInclude(e => e.Exercise)
                .Where(p => p.MemberProfileId == profile.Id && !p.IsDeleted)
                .OrderByDescending(p => p.IsActive)
                .ThenByDescending(p => p.CreatedAt)
                .Take(5)
                .ToListAsync();

            var nutritionPlans = await _context.NutritionPlans
                .Include(p => p.Meals)
                .Where(p => p.MemberProfileId == profile.Id && !p.IsDeleted)
                .OrderByDescending(p => p.IsActive)
                .ThenByDescending(p => p.CreatedAt)
                .Take(5)
                .ToListAsync();

            var context = UserContextBuilder.Build(
                profile,
                nutritionPlans: nutritionPlans,
                workoutPlans: workoutPlans);

            var historyText = history.Count == 0
                ? "No previous messages in this conversation."
                : string.Join("\n", history.Select(m => $"{m.Sender}: {m.MessageText}"));

            return $"""
            {context}

            === RECENT CONVERSATION HISTORY ===
            {historyText}

            === REFERENCE RESOLUTION RULES ===
            When the member says this plan, this workout, this exercise, this meal, previous plan, previous workout, that exercise, or my plan, connect it to the latest active saved plan above.
            Never create a new workout or nutrition plan unless the member explicitly asks to generate or create one.
            """;
        }

        private async Task<string> GetActiveWorkoutPlanContextAsync(Guid memberProfileId)
        {
            var plan = await _context.WorkoutPlans
                .Include(p => p.WorkoutDays)
                    .ThenInclude(d => d.Exercises)
                        .ThenInclude(e => e.Exercise)
                .Where(p => p.MemberProfileId == memberProfileId && p.IsActive && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            if (plan == null)
                return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine($"Plan: {plan.Name}");
            sb.AppendLine($"Duration: {plan.DurationWeeks} weeks");
            foreach (var day in plan.WorkoutDays.OrderBy(d => d.DayNumber))
            {
                sb.AppendLine($"Day {day.DayNumber} - {day.DayName}");
                foreach (var exercise in day.Exercises)
                {
                    var name = !string.IsNullOrWhiteSpace(exercise.ExrciseName)
                        ? exercise.ExrciseName
                        : exercise.Exercise?.Name ?? "Exercise";
                    sb.AppendLine($"- {name}: {exercise.Sets} sets x {exercise.Reps} reps, rest {exercise.RestSeconds ?? 0}s, notes: {exercise.Notes ?? "None"}");
                }
            }

            return sb.ToString();
        }

        private static string NormalizeIntent(string? detectedIntent, string userMessage, List<ChatMessageDto> history)
        {
            var text = userMessage.ToLowerInvariant();
            var normalized = detectedIntent?.Trim();
            normalized = normalized switch
            {
                "GENERATE_WORKOUT_PLAN" => "workout",
                "GENERATE_NUTRITION_PLAN" => "nutrition",
                "MODIFY_WORKOUT_PLAN" => "chat",
                "MODIFY_NUTRITION_PLAN" => "chat",
                "ASK_ABOUT_INJURY_COMPATIBILITY" => "ask_about_injury_compatibility",
                "ASK_ABOUT_EXERCISE" => "chat",
                "ASK_ABOUT_NUTRITION" => "chat",
                "REQUEST_ALTERNATIVE_EXERCISE" => "chat",
                "REQUEST_MEAL_SUGGESTION" => "food_analysis",
                "REQUEST_FOOD_ANALYSIS" => "food_analysis",
                "BOOK_SESSION" => "booking",
                "ASK_BOOKING_DETAILS" => "booking",
                "GENERAL_FITNESS_QUESTION" => "chat",
                "GREETING" => "chat",
                _ => string.IsNullOrWhiteSpace(normalized) ? "chat" : normalized
            };

            if (ContainsAny(text,
                "suitable for my injury", "safe for my injury", "okay for my injury", "with my injury", "injury compatible",
                "hurt my", "bad for my knee", "bad for my back",
                "\u064a\u0646\u0627\u0633\u0628 \u0627\u0635\u0627\u0628\u062a\u064a", "\u0645\u0646\u0627\u0633\u0628 \u0644\u0627\u0635\u0627\u0628\u062a\u064a", "\u064a\u0646\u0641\u0639 \u0645\u0639 \u0627\u0644\u0627\u0635\u0627\u0628\u0629", "\u0622\u0645\u0646 \u0644\u0627\u0635\u0627\u0628\u062a\u064a"))
                return "ask_about_injury_compatibility";

            if (ContainsAny(text, "this plan", "this workout", "previous plan", "previous workout", "my plan", "that exercise",
                "\u0627\u0644\u062e\u0637\u0629 \u062f\u064a", "\u0627\u0644\u062a\u0645\u0631\u064a\u0646 \u062f\u0647", "\u062e\u0637\u062a\u064a", "\u0627\u0644\u062e\u0637\u0629 \u0627\u0644\u0644\u064a \u0641\u0627\u062a\u062a"))
            {
                if (ContainsAny(text, "injury", "injuries", "pain", "knee", "back", "\u0627\u0635\u0627\u0628\u0629", "\u0627\u0644\u0627\u0635\u0627\u0628\u0629", "\u0648\u062c\u0639", "\u0631\u0643\u0628\u0629", "\u0638\u0647\u0631"))
                    return "ask_about_injury_compatibility";

                if (normalized is "workout" or "nutrition" or "both")
                    return "chat";
            }

            return normalized;
        }

        /// <summary>
        /// Returns true when the message mentions specific food items (so the food-analysis
        /// JSON pipeline is appropriate). Returns false for general questions like
        /// "what should I eat before training?" where a direct coaching reply is better.
        /// </summary>
        private static bool UserMentionsSpecificFoods(string userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
                return false;

            var text = userMessage.ToLowerInvariant();

            // Arabic/English patterns that indicate "I have X food" or "can I eat X"
            // These trigger the food-analysis pipeline.
            var specificFoodPatterns = new[]
            {
                "عندي ", "عندى ", "عنده ", "i have ", "i've got ", "i got ",
                "لدي ", "عندنا ", "can i eat ", "is it ok to eat ", "is it okay to eat ",
                "هاكل ", "هآكل ", "ممكن اكل ", "ممكن آكل ",
                "كام جرام ", "كام غرام ", "how many grams", "how much protein in",
                "كالوريز ", "كالوريه ", "calories in ", "protein in ",
                "بيض", "فراخ", "أرز", "رز", "جبن", "لبن", "موز", "تونة", "تونه",
                "chicken", "rice", "eggs", "banana", "tuna", "oats", "milk", "cheese",
                "bread", "عيش", "خبز", "pasta", "مكرونة", "sweet potato", "بطاطا",
                "pizza", "burger", "فول", "عدس", "زبادي", "yogurt", "peanut butter",
                "nuts", "مكسرات", "whey", "protein shake", "protein bar"
            };

            return specificFoodPatterns.Any(p => text.Contains(p));
        }

        private static string FormatUserVisibleReply(string reply, string? intent, bool isArabic)
        {
            if (string.IsNullOrWhiteSpace(reply))
                return reply;

            var cleaned = Regex.Replace(reply, @"<PLAN_DATA>\s*{.*?}\s*</PLAN_DATA>", string.Empty, RegexOptions.Singleline).Trim();

            if (cleaned.Contains("\"foodPlan\"", StringComparison.OrdinalIgnoreCase))
                return FormatNutritionJsonReply(cleaned, true);

            if (LooksLikeJson(cleaned))
            {
                if (intent == "food_analysis" || cleaned.Contains("\"foodPlan\"", StringComparison.OrdinalIgnoreCase))
                    return FormatNutritionJsonReply(cleaned, true);

                return isArabic
                    ? "\u062a\u0645\u0627\u0645\u060c \u062d\u0644\u0644\u062a \u0627\u0644\u0628\u064a\u0627\u0646\u0627\u062a \u0648\u062c\u0647\u0632\u062a \u0644\u0643 \u0627\u0644\u0646\u062a\u064a\u062c\u0629 \u0628\u0634\u0643\u0644 \u0648\u0627\u0636\u062d. \u0644\u0648 \u062d\u0627\u0628\u0628 \u062a\u0641\u0627\u0635\u064a\u0644 \u0623\u0643\u062b\u0631\u060c \u0627\u0633\u0623\u0644\u0646\u064a \u0639\u0646 \u0623\u064a \u062c\u0632\u0621."
                    : "I processed the result and can walk you through any part in simple terms.";
            }

            return cleaned;
        }

        private static bool LooksLikeJson(string value)
        {
            var trimmed = value.Trim();
            return (trimmed.StartsWith("{") && trimmed.EndsWith("}")) ||
                   (trimmed.StartsWith("[") && trimmed.EndsWith("]"));
        }

        private static string FormatNutritionJsonReply(string rawReply, bool forceArabic)
        {
            var clean = ExtractJsonObject(rawReply);
            try
            {
                using var doc = JsonDocument.Parse(clean);
                var root = doc.RootElement;
                if (!root.TryGetProperty("foodPlan", out var foodPlan) || foodPlan.ValueKind != JsonValueKind.Array)
                    return BuildJsonBlockedFallback();

                var sb = new StringBuilder();
                AppendMealSection(sb, "\ud83c\udf73 \u0627\u0644\u0641\u0637\u0627\u0631", foodPlan, "breakfast");
                AppendMealSection(sb, "\ud83c\udf57 \u0627\u0644\u063a\u062f\u0627\u0621", foodPlan, "lunch");
                AppendMealSection(sb, "\ud83c\udfcb\ufe0f \u0642\u0628\u0644 \u0627\u0644\u062a\u0645\u0631\u064a\u0646", foodPlan, "pre-workout");
                AppendMealSection(sb, "\ud83d\udcaa \u0628\u0639\u062f \u0627\u0644\u062a\u0645\u0631\u064a\u0646", foodPlan, "post-workout");

                if (sb.Length == 0)
                    AppendMealSection(sb, "\ud83c\udf7d\ufe0f \u0627\u0644\u0648\u062c\u0628\u0629 \u0627\u0644\u0645\u0642\u062a\u0631\u062d\u0629", foodPlan, null);

                if (root.TryGetProperty("totals", out var totals))
                {
                    sb.AppendLine("\ud83d\udcca \u0645\u0644\u062e\u0635 \u0627\u0644\u064a\u0648\u0645");
                    sb.AppendLine($"\u0627\u0644\u0633\u0639\u0631\u0627\u062a: {GetNumber(totals, "calories")} \u0633\u0639\u0631");
                    sb.AppendLine($"\u0627\u0644\u0628\u0631\u0648\u062a\u064a\u0646: {GetNumber(totals, "proteinGrams")} \u062c\u0631\u0627\u0645");
                    sb.AppendLine($"\u0627\u0644\u0643\u0627\u0631\u0628\u0648\u0647\u064a\u062f\u0631\u0627\u062a: {GetNumber(totals, "carbsGrams")} \u062c\u0631\u0627\u0645");
                    sb.AppendLine($"\u0627\u0644\u062f\u0647\u0648\u0646: {GetNumber(totals, "fatGrams")} \u062c\u0631\u0627\u0645");
                    sb.AppendLine();
                }

                sb.AppendLine("\ud83d\udca1 \u062a\u0648\u0635\u064a\u0629 \u0627\u0644\u0643\u0648\u062a\u0634");
                if (root.TryGetProperty("sufficiencyAssessment", out var assessment) &&
                    assessment.TryGetProperty("summary", out var summary))
                    sb.AppendLine(summary.GetString());

                if (root.TryGetProperty("recommendations", out var recommendations) && recommendations.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in recommendations.EnumerateArray())
                        sb.AppendLine($"- {item.GetString()}");
                }

                return sb.ToString().Trim();
            }
            catch
            {
                return BuildJsonBlockedFallback();
            }
        }

        private static string ExtractJsonObject(string rawReply)
        {
            var clean = AIHelper.CleanJson(rawReply);
            if (LooksLikeJson(clean))
                return clean;

            var start = rawReply.IndexOf('{');
            var end = rawReply.LastIndexOf('}');
            return start >= 0 && end > start
                ? rawReply.Substring(start, end - start + 1).Trim()
                : clean;
        }

        private static string BuildJsonBlockedFallback() =>
            "\u062d\u0644\u0644\u062a \u0627\u0644\u0623\u0643\u0644 \u0644\u0643\u060c \u0628\u0633 \u0645\u062d\u062a\u0627\u062c \u0623\u0639\u064a\u062f \u0635\u064a\u0627\u063a\u0629 \u0627\u0644\u0646\u062a\u064a\u062c\u0629 \u0628\u0634\u0643\u0644 \u0623\u0648\u0636\u062d. \u0642\u0648\u0644\u064a \u0627\u0644\u0623\u0643\u0644 \u0627\u0644\u0645\u062a\u0627\u062d \u0639\u0646\u062f\u0643 \u0648\u0647\u0623\u0631\u062a\u0628\u0647 \u0644\u0643 \u0643\u0648\u062c\u0628\u0629 \u0639\u0631\u0628\u064a\u0629 \u0648\u0627\u0636\u062d\u0629.";

        private static void AppendMealSection(StringBuilder sb, string title, JsonElement foodPlan, string? mealKey)
        {
            var items = foodPlan.EnumerateArray()
                .Where(item => mealKey == null ||
                               (item.TryGetProperty("mealTiming", out var timing) &&
                                timing.GetString()?.Contains(mealKey, StringComparison.OrdinalIgnoreCase) == true))
                .ToList();

            if (items.Count == 0)
                return;

            sb.AppendLine(title);
            foreach (var item in items)
            {
                sb.AppendLine($"{GetString(item, "foodName")} - {GetString(item, "recommendedAmount")}");
                sb.AppendLine($"\u0627\u0644\u0637\u0631\u064a\u0642\u0629: {GetString(item, "reason")}");
                sb.AppendLine($"\u0627\u0644\u0633\u0639\u0631\u0627\u062a: {GetNumber(item, "calories")} | \u0628\u0631\u0648\u062a\u064a\u0646: {GetNumber(item, "proteinGrams")}g | \u0643\u0627\u0631\u0628: {GetNumber(item, "carbsGrams")}g");
            }
            sb.AppendLine("---");
        }

        private static string GetString(JsonElement element, string propertyName) =>
            element.TryGetProperty(propertyName, out var property) ? property.GetString() ?? "-" : "-";

        private static string GetNumber(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var property))
                return "0";

            return property.ValueKind == JsonValueKind.Number && property.TryGetDecimal(out var value)
                ? value.ToString("0.##", CultureInfo.InvariantCulture)
                : property.ToString();
        }

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

            var welcomeMessageText = "👋 Welcome to Arena AI Coach! I'm here to help you crush your fitness goals. Ask me about training, nutrition, recovery, or gym bookings. How can I help you today?";
            _context.ChatMessages.Add(new ChatMessage
            {
                ChatConversationId = conversation.Id,
                MessageText = welcomeMessageText,
                Sender = SenderType.AI,
                Intent = "welcome",
                SentAt = DateTime.UtcNow.AddMilliseconds(-1)
            });
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

        private static bool IsProfileMemoryQuestion(string userMessage)
        {
            var text = userMessage.Trim().ToLowerInvariant();
            var asksKnownInfo = ContainsAny(text,
                "do you know", "did you know", "you know", "remember", "my profile", "my info", "my data", "what are my", "tell me my",
                "\u0627\u0646\u062a \u0639\u0627\u0631\u0641", "\u0627\u0646\u062a\u064a \u0639\u0627\u0631\u0641\u0629", "\u0639\u0627\u0631\u0641", "\u0641\u0627\u0643\u0631", "\u0641\u0627\u0643\u0631\u0629", "\u0627\u0644\u0645\u0644\u0641", "\u0628\u0631\u0648\u0641\u0627\u064a\u0644", "\u0628\u064a\u0627\u0646\u0627\u062a\u064a");
            var asksAboutMemberData = ContainsAny(text,
                "injury", "injuries", "pain", "condition", "conditions", "health", "disease", "diseases", "goal", "weight", "height", "equipment", "experience",
                "\u0627\u0635\u0627\u0628\u0629", "\u0627\u0635\u0627\u0628\u0627\u062a", "\u0627\u0644\u0627\u0635\u0627\u0628\u0627\u062a", "\u0627\u0644\u0625\u0635\u0627\u0628\u0627\u062a", "\u0648\u062c\u0639", "\u0627\u0644\u0645", "\u0635\u062d\u0629", "\u0627\u0645\u0631\u0627\u0636", "\u0623\u0645\u0631\u0627\u0636", "\u0647\u062f\u0641", "\u0648\u0632\u0646", "\u0637\u0648\u0644", "\u0645\u0639\u062f\u0627\u062a", "\u062e\u0628\u0631\u0629");
            var asksToCreatePlan = ContainsAny(text,
                "generate", "create", "make me", "build me", "plan", "program", "meal plan", "workout plan",
                "\u0627\u0639\u0645\u0644", "\u0627\u0639\u0645\u0644\u064a", "\u0627\u0639\u0645\u0644\u0644\u064a", "\u062e\u0637\u0629", "\u0628\u0631\u0646\u0627\u0645\u062c", "\u0646\u0638\u0627\u0645");

            return asksKnownInfo && asksAboutMemberData && !asksToCreatePlan;
        }

        private static bool IsSimpleGreeting(string userMessage)
        {
            var text = Regex.Replace(userMessage.Trim().ToLowerInvariant(), @"[!?.\s]+", " ").Trim();
            if (string.IsNullOrWhiteSpace(text))
                return false;

            return text is "hi" or "hello" or "hey" or "yo" or "sup" or "good morning" or "good afternoon" or "good evening"
                or "\u0627\u0647\u0644\u0627" or "\u0627\u0647\u0644\u0627 \u0648\u0633\u0647\u0644\u0627" or "\u0645\u0631\u062d\u0628\u0627" or "\u0647\u0627\u064a"
                || Regex.IsMatch(text, @"^(hi|hello|hey)\s+(there|arena|coach|assistant)$", RegexOptions.IgnoreCase);
        }

        private static IntentResult? DetectSimpleIntent(string userMessage)
        {
            var text = userMessage.ToLowerInvariant();

            var asksWorkout = ContainsAny(text,
                "workout", "exercise", "training plan", "fitness plan", "gym", "gym plan", "days a week", "times a week",
                "\u062a\u0645\u0631\u064a\u0646", "\u062a\u062f\u0631\u064a\u0628", "\u062c\u064a\u0645", "\u0627\u0644\u062c\u064a\u0645", "\u062e\u0637\u0629 \u062a\u0645\u0631\u064a\u0646", "\u062e\u0637\u0629 \u062a\u062f\u0631\u064a\u0628");
            var asksNutrition = ContainsAny(text,
                "nutrition", "meal plan", "diet", "calories", "food plan", "food", "meal",
                "\u063a\u0630\u0627\u0621", "\u063a\u0630\u0627\u0626\u064a\u0629", "\u062a\u063a\u0630\u064a\u0629", "\u0648\u062c\u0628\u0627\u062a", "\u0648\u062c\u0628\u0629", "\u0627\u0643\u0644", "\u0623\u0643\u0644", "\u062f\u0627\u064a\u062a", "\u0633\u0639\u0631\u0627\u062a", "\u0646\u0638\u0627\u0645 \u063a\u0630\u0627\u0626\u064a", "\u062e\u0637\u0629 \u063a\u0630\u0627\u0626\u064a\u0629", "\u062e\u0637\u0629 \u062a\u063a\u0630\u064a\u0629");
            var asksGenericPlan = ContainsAny(text,
                "wanna a plan", "want a plan", "need a plan", "make me a plan", "create a plan", "generate a plan",
                "give me a plan", "build me a plan", "plan for me", "custom plan", "personalized plan", "personalised plan",
                "\u0627\u0639\u0645\u0644 \u062e\u0637\u0629", "\u0627\u0639\u0645\u0644\u064a \u062e\u0637\u0629", "\u0627\u0639\u0645\u0644\u0644\u064a \u062e\u0637\u0629", "\u0639\u0627\u064a\u0632 \u062e\u0637\u0629", "\u0639\u0627\u064a\u0632\u0629 \u062e\u0637\u0629", "\u062e\u0637\u0629 \u0627\u0645\u0634\u064a \u0639\u0644\u064a\u0647\u0627");

            var preferredDuration = TryFindDuration(text, out var detectedDuration) ? detectedDuration : null;
            var equipment = DetectEquipmentFromMessage(text);

            if (ContainsAny(text, "both", "workout and nutrition", "nutrition and workout", "meal and workout", "diet and workout", "\u0627\u0644\u0627\u062a\u0646\u064a\u0646", "\u0643\u0644\u0647")
                || (asksWorkout && asksNutrition))
                return new IntentResult { Intent = "both", PreferredDuration = preferredDuration, Equipment = equipment };

            if (asksWorkout)
                return new IntentResult { Intent = "workout", PreferredDuration = preferredDuration, Equipment = equipment };

            var goalChangeKeywords = ContainsAny(text,
                "change my goal", "switch goal", "new goal", "i want to gain", "i wanna gain",
                "i want to lose", "i wanna lose", "my goal is", "my new goal");
            if (goalChangeKeywords && !asksWorkout)
                return new IntentResult { Intent = "goal_change" };

            if (asksNutrition)
                return new IntentResult { Intent = "nutrition", PreferredDuration = preferredDuration, Equipment = equipment };

            if (asksGenericPlan)
                return new IntentResult { Intent = "both", PreferredDuration = preferredDuration, Equipment = equipment };

            if (ContainsAny(text, "food analysis", "analyze food", "\u062a\u062d\u0644\u064a\u0644 \u0627\u0644\u0627\u0643\u0644", "\u062d\u0644\u0644 \u0627\u0644\u0627\u0643\u0644"))
                return new IntentResult { Intent = "food_analysis" };

            // Asking which times/slots are available/open to book \u2192 booking (no action, list the day's slots).
            if (ContainsAny(text, "available time", "available times", "available slot", "available slots",
                "available date", "available dates", "open slot", "open slots", "what times", "which times",
                "\u0627\u0644\u0623\u0648\u0642\u0627\u062a \u0627\u0644\u0645\u062a\u0627\u062d\u0629", "\u0627\u0644\u0645\u0648\u0627\u0639\u064a\u062f \u0627\u0644\u0645\u062a\u0627\u062d\u0629", "\u0645\u0648\u0627\u0639\u064a\u062f \u0645\u062a\u0627\u062d\u0629"))
                return new IntentResult { Intent = "booking" };

            if (!ContainsAny(text, "book", "booking", "reserve", "cancel", "reschedule", "\u0627\u062d\u062c\u0632", "\u062d\u062c\u0632", "\u0627\u0644\u063a\u0627\u0621", "\u0625\u0644\u063a\u0627\u0621"))
                return new IntentResult { Intent = "chat" };

            return null;
        }

        private static string? DetectEquipmentFromMessage(string text)
        {
            if (ContainsAny(text, "full gym", "gym", "at the gym", "go to gym", "go to the gym", "\u062c\u064a\u0645", "\u0627\u0644\u062c\u064a\u0645"))
                return "Full Gym";

            if (ContainsAny(text, "home", "home workout", "at home", "\u0627\u0644\u0628\u064a\u062a", "\u0641\u064a \u0627\u0644\u0628\u064a\u062a"))
                return "Home workout";

            if (ContainsAny(text, "dumbbell", "dumbbells", "weights", "home weights", "\u062f\u0645\u0628\u0644", "\u0627\u0648\u0632\u0627\u0646", "\u0623\u0648\u0632\u0627\u0646"))
                return "Dumbbells / weights";

            if (ContainsAny(text, "no equipment", "bodyweight", "without equipment", "\u0628\u062f\u0648\u0646 \u0645\u0639\u062f\u0627\u062a", "\u0645\u0646 \u063a\u064a\u0631 \u0645\u0639\u062f\u0627\u062a"))
                return "No equipment";

            return null;
        }

        private static string? DetectGoalFromMessage(string userMessage)
        {
            var text = userMessage.ToLowerInvariant();
            if (ContainsAny(text, "gain weight", "weight gain", "bulk", "muscle gain", "build muscle", "اكسب وزن", "ازيد وزن", "اضخم"))
                return "Weight Gain / Muscle Gain";
            if (ContainsAny(text, "lose weight", "weight loss", "fat loss", "cut", "اخس", "انحف", "تنشيف"))
                return "Weight Loss";
            if (ContainsAny(text, "endurance", "fitness", "fit", "لياقة"))
                return "General Fitness";
            return null;
        }

        private static IntentResult? DetectPlanClarificationIntent(
            string userMessage,
            List<ChatMessageDto> history)
        {
            var text = userMessage.Trim().ToLowerInvariant();
            var isDurationOnlyReply = TryFindDuration(text, out var durationFromCurrentMessage);
            var isNoneReply = ContainsAny(text,
                "none", "no", "nope", "nothing", "no allergies", "no restrictions", "not any", "healthy", "i am healthy", "im healthy",
                "\u0644\u0627", "\u0644\u0627 \u064a\u0648\u062c\u062f", "\u0645\u0641\u064a\u0634", "\u0645\u0627\u0641\u064a\u0634", "\u0628\u062f\u0648\u0646", "\u0633\u0644\u064a\u0645", "\u0633\u0644\u064a\u0645\u0629", "\u0633\u0644\u064a\u0645 \u062a\u0645\u0627\u0645\u0627", "\u0633\u0644\u064a\u0645 \u062a\u0645\u0627\u0645\u0627\u064b", "سليم");
            var looksLikePlanKeyword = ContainsAny(text,
                "both", "workout", "nutrition", "meal", "diet", "food", "gym",
                "\u0627\u0644\u0627\u062a\u0646\u064a\u0646", "\u0643\u0644\u0647", "\u062a\u063a\u0630\u064a\u0629", "\u063a\u0630\u0627\u0626\u064a\u0629", "\u0648\u062c\u0628\u0629", "\u0648\u062c\u0628\u0627\u062a", "\u0646\u0638\u0627\u0645 \u063a\u0630\u0627\u0626\u064a", "\u062a\u0645\u0631\u064a\u0646", "\u062c\u064a\u0645", "\u0627\u0644\u062c\u064a\u0645");

            var recentAssistantMessage = history
                .Where(m => m.Sender == "assistant")
                .Reverse()
                .Take(6)
                .Select(m => m.MessageText.ToLowerInvariant())
                .FirstOrDefault(message => ContainsAny(message,
                    "workout plan, a nutrition plan, or both", "workout plan", "nutrition plan", "or both",
                    "preferred plan duration", "dietary restrictions", "food allergies", "i need to know",
                    "first need to know", "injuries or health conditions",
                    "\u062e\u0637\u0629 \u062a\u0645\u0631\u064a\u0646", "\u062e\u0637\u0629 \u062a\u063a\u0630\u064a\u0629", "\u062e\u0637\u0629 \u063a\u0630\u0627\u0626\u064a\u0629", "\u0645\u062f\u0629 \u0627\u0644\u062e\u0637\u0629", "\u062d\u0633\u0627\u0633\u064a\u0629", "\u0646\u0638\u0627\u0645 \u063a\u0630\u0627\u0626\u064a", "\u0627\u0644\u0627\u062a\u0646\u064a\u0646", "\u0625\u0635\u0627\u0628\u0627\u062a \u0623\u0648 \u0623\u0645\u0631\u0627\u0636"));

            if (recentAssistantMessage == null && !isDurationOnlyReply && !isNoneReply && !looksLikePlanKeyword)
                return null;

            var asksWorkout = ContainsAny(text,
                "workout", "exercise", "training", "gym",
                "\u062a\u0645\u0631\u064a\u0646", "\u062c\u064a\u0645", "\u0627\u0644\u062c\u064a\u0645", "\u062e\u0637\u0629 \u062a\u0645\u0631\u064a\u0646", "\u062e\u0637\u0629 \u062a\u062f\u0631\u064a\u0628");
            var asksNutrition = ContainsAny(text,
                "nutrition", "meal", "diet", "food",
                "\u062a\u063a\u0630\u064a\u0629", "\u063a\u0630\u0627\u0626\u064a\u0629", "\u0648\u062c\u0628\u0629", "\u0648\u062c\u0628\u0627\u062a", "\u062f\u0627\u064a\u062a", "\u0646\u0638\u0627\u0645 \u063a\u0630\u0627\u0626\u064a", "\u062e\u0637\u0629 \u063a\u0630\u0627\u0626\u064a\u0629", "\u062e\u0637\u0629 \u062a\u063a\u0630\u064a\u0629");

            var recentUserPlanRequest = history
                .Where(m => m.Sender == "user")
                .Reverse()
                .Take(8)
                .Select(m => m.MessageText.ToLowerInvariant())
                .FirstOrDefault(message => ContainsAny(message,
                    "workout", "exercise", "training", "nutrition", "meal", "diet", "food", "gym",
                    "\u062a\u0645\u0631\u064a\u0646", "\u062a\u063a\u0630\u064a\u0629", "\u063a\u0630\u0627\u0626\u064a\u0629", "\u0648\u062c\u0628\u0629", "\u0648\u062c\u0628\u0627\u062a", "\u0646\u0638\u0627\u0645 \u063a\u0630\u0627\u0626\u064a", "\u062e\u0637\u0629 \u063a\u0630\u0627\u0626\u064a\u0629", "\u062e\u0637\u0629 \u062a\u063a\u0630\u064a\u0629", "\u062c\u064a\u0645", "\u0627\u0644\u062c\u064a\u0645"));

            if (recentUserPlanRequest != null)
            {
                asksWorkout = asksWorkout || ContainsAny(recentUserPlanRequest,
                    "workout", "exercise", "training", "gym",
                    "\u062a\u0645\u0631\u064a\u0646", "\u062c\u064a\u0645", "\u0627\u0644\u062c\u064a\u0645", "\u062e\u0637\u0629 \u062a\u0645\u0631\u064a\u0646", "\u062e\u0637\u0629 \u062a\u062f\u0631\u064a\u0628");
                asksNutrition = asksNutrition || ContainsAny(recentUserPlanRequest,
                    "nutrition", "meal", "diet", "food",
                    "\u062a\u063a\u0630\u064a\u0629", "\u063a\u0630\u0627\u0626\u064a\u0629", "\u0648\u062c\u0628\u0629", "\u0648\u062c\u0628\u0627\u062a", "\u0646\u0638\u0627\u0645 \u063a\u0630\u0627\u0626\u064a", "\u062e\u0637\u0629 \u063a\u0630\u0627\u0626\u064a\u0629", "\u062e\u0637\u0629 \u062a\u063a\u0630\u064a\u0629");
            }

            if (recentAssistantMessage != null && ContainsAny(recentAssistantMessage, "dietary restrictions", "food allergies", "\u062e\u0637\u0629 \u062a\u063a\u0630\u064a\u0629", "\u062e\u0637\u0629 \u063a\u0630\u0627\u0626\u064a\u0629", "\u062d\u0633\u0627\u0633\u064a\u0629", "\u0646\u0638\u0627\u0645 \u063a\u0630\u0627\u0626\u064a"))
                asksNutrition = true;
            
            if (recentAssistantMessage != null && ContainsAny(recentAssistantMessage, "injuries or health conditions", "\u062e\u0637\u0629 \u062a\u0645\u0631\u064a\u0646", "\u0625\u0635\u0627\u0628\u0627\u062a \u0623\u0648 \u0623\u0645\u0631\u0627\u0636"))
                asksWorkout = true;

            var preferredDuration = isDurationOnlyReply
                ? durationFromCurrentMessage
                : FindRecentDuration(history);

            var intent = asksWorkout && asksNutrition ? "both" : asksWorkout ? "workout" : asksNutrition ? "nutrition" : null;
            if (intent == null)
                return null;

            return new IntentResult
            {
                Intent = intent,
                PreferredDuration = preferredDuration,
                DietaryRestrictions = isNoneReply ? "None" : null,
                Injuries = isNoneReply ? "None" : null,
                HealthConditions = isNoneReply ? "None" : null
            };
        }

        private static string? FindRecentDuration(List<ChatMessageDto> history)
        {
            foreach (var message in history.Where(m => m.Sender == "user").Reverse().Take(8))
            {
                if (TryFindDuration(message.MessageText.Trim().ToLowerInvariant(), out var duration))
                    return duration;
            }

            return null;
        }

        private static bool TryFindDuration(string text, out string? duration)
        {
            duration = null;
            var match = Regex.Match(text, @"\b\d+\s*(week|weeks|month|months|day|days)\b", RegexOptions.IgnoreCase);
            if (!match.Success)
                match = Regex.Match(text, @"\d+\s*(\u0627\u0633\u0628\u0648\u0639|\u0623\u0633\u0627\u0628\u064a\u0639|\u0627\u0633\u0627\u0628\u064a\u0639|\u0634\u0647\u0631|\u0634\u0647\u0648\u0631|\u064a\u0648\u0645|\u0627\u064a\u0627\u0645|\u0623\u064a\u0627\u0645)", RegexOptions.IgnoreCase);

            if (!match.Success)
                return false;

            duration = match.Value.Trim();
            return true;
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
                    "date and time of the booking you want to cancel",
                    "booking date and the new time"));

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
            var today = DateTime.UtcNow.AddHours(3).Date;

            if (ContainsAny(normalized, "after tomorrow", "بعد بكرة", "بعد بكره"))
            {
                date = today.AddDays(2);
                return true;
            }

            if (ContainsAny(normalized, "tomorrow", "بكرة", "بكره"))
            {
                date = today.AddDays(1);
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

            var egyptToday = DateTime.UtcNow.AddHours(3).Date;
            var slotReplyText = lastSlotReply.MessageText.ToLowerInvariant();

            var dateFromLabel = TryParseDateFromSlotLabel(slotReplyText, egyptToday);
            if (dateFromLabel.HasValue)
                return dateFromLabel.Value;

            if (ContainsAny(slotReplyText, "after tomorrow", "بعد بكرة", "بعد بكره"))
                return egyptToday.AddDays(2);

            if (ContainsAny(slotReplyText, "tomorrow", "بكرة", "بكره"))
                return egyptToday.AddDays(1);

            if (ContainsAny(slotReplyText, "today", "النهارده", "النهاردة", "اليوم"))
                return egyptToday;

            var weekday = DetectWeekday(slotReplyText);
            if (weekday.HasValue)
                return NextOrSameWeekday(egyptToday, weekday.Value);

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

            var welcomeMessageText = "👋 Welcome to Arena AI Coach!\n\nBefore I create any workout or nutrition plan, please tell me about any medical conditions, injuries, allergies, medications, physical limitations, or dietary restrictions you have.";
            _context.ChatMessages.Add(new ChatMessage
            {
                ChatConversationId = conversation.Id,
                MessageText = welcomeMessageText,
                Sender = SenderType.AI,
                Intent = "welcome",
                SentAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            return new ConversationDto
            {
                Id = conversation.Id,
                Title = conversation.Title,
                StartedAt = conversation.StartedAt,
                MessageCount = 1,
                LastMessage = welcomeMessageText
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
