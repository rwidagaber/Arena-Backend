using ArenaApplication.AI.ArenaApplication.AI;
using ArenaApplication.AI.Planning;
using ArenaApplication.Dtos.ChatDtos;
using ArenaApplication.Dtos.HealthIntelligence;
using ArenaApplication.Dtos.Nutrition;
using ArenaApplication.Dtos.WorkoutPlan;
using ArenaApplication.Dtos.Attendance;
using ArenaApplication.IServices;
using ArenaDomain.Entities;
using ArenaDomain.Entities.Chat;
using ArenaDomain.Shared;
using ArenaDomain.Entities.Bookings;
using ArenaDomain.Entities.User;
using ArenaDomain.Enums;
using ArenaDomain.Interfaces;
using ArenaInfrastructure.AI;
using ArenaInfrastructure.Data;
using ArenaInfrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace ArenaTests
{
    public class TrackingWorkoutAIService : IWorkoutAIService
    {
        public string LastUserMessage { get; set; } = string.Empty;

        public Task<WorkoutPlanDto> GenerateWorkoutPlanAsync(Guid memberProfileId, string userMessage)
        {
            LastUserMessage = userMessage;
            return Task.FromResult(new WorkoutPlanDto { Name = "Custom Workout Plan" });
        }

        public Task<WorkoutPlanDto> ModifyWorkoutPlanAsync(Guid memberProfileId, string userMessage)
        {
            LastUserMessage = userMessage;
            return Task.FromResult(new WorkoutPlanDto { Name = "Modified Workout Plan" });
        }
    }

    public class TrackingNutritionAIService : INutritionAIService
    {
        public string LastUserMessage { get; set; } = string.Empty;

        public Task<NutritionPlanResponseDto> GenerateNutritionPlanAsync(Guid memberProfileId, string userMessage)
        {
            LastUserMessage = userMessage;
            return Task.FromResult(new NutritionPlanResponseDto());
        }

        public Task<NutritionPlanResponseDto> ModifyNutritionPlanAsync(Guid memberProfileId, string userMessage)
        {
            LastUserMessage = userMessage;
            return Task.FromResult(new NutritionPlanResponseDto());
        }
    }

    public class ConversationalUnderstandingGeminiService : IGeminiCompletionService
    {
        public Task<string> GetCompletionAsync(string systemPrompt, List<ChatMessageDto> history, string userMessage)
        {
            var prompt = (systemPrompt ?? "").ToLowerInvariant() + " " + (userMessage ?? "").ToLowerInvariant();

            if (prompt.Contains("intent detection") || prompt.Contains("intent_detection"))
            {
                var combinedText = (userMessage + " " + string.Join(" ", history.Select(h => h.MessageText))).ToLowerInvariant();

                if (combinedText.Contains("generate a nutrition plan"))
                {
                    return Task.FromResult("{\"intent\": \"GENERATE_NUTRITION_PLAN\"}");
                }
                if (combinedText.Contains("adjust your daily nutrition targets"))
                {
                    return Task.FromResult("{\"intent\": \"MODIFY_NUTRITION_PLAN\"}");
                }
                if (combinedText.Contains("modify your workout") || combinedText.Contains("replace those exercises"))
                {
                    return Task.FromResult("{\"intent\": \"MODIFY_WORKOUT_PLAN\"}");
                }
                if (combinedText.Contains("خطة غذائية"))
                {
                    return Task.FromResult("{\"intent\": \"GENERATE_NUTRITION_PLAN\"}");
                }
                return Task.FromResult("{\"intent\": \"chat\"}");
            }

            return Task.FromResult("Mock Gemini response");
        }

        public Task<string> GetVisionCompletionAsync(string systemPrompt, string userMessage, string imageMimeType, string imageBase64) 
            => Task.FromResult(string.Empty);

        public Task<string> TranscribeAudioAsync(string audioMimeType, string audioBase64) 
            => Task.FromResult(string.Empty);
    }

    public class ConversationalUnderstandingTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly AppDbContext _context;
        private readonly ChatService _chatService;
        private readonly TrackingWorkoutAIService _workoutAI;
        private readonly TrackingNutritionAIService _nutritionAI;
        private readonly Guid _memberId;

        public ConversationalUnderstandingTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;

            _context = new AppDbContext(options);
            _context.Database.EnsureCreated();

            var userId = Guid.NewGuid();
            var appUser = new ApplicationUser
            {
                Id = userId,
                UserName = "testuser",
                Email = "test@example.com"
            };
            _context.Users.Add(appUser);

            _memberId = Guid.NewGuid();
            var profile = new MemberProfile
            {
                Id = _memberId,
                UserId = userId,
                FirstName = "TestMember",
                DateOfBirth = DateTime.UtcNow.AddYears(-25),
                Gender = Gender.Male,
                User = appUser
            };
            _context.MemberProfiles.Add(profile);
            _context.SaveChanges();

            var geminiMock = new ConversationalUnderstandingGeminiService();
            var embeddingMock = new MockEmbeddingService();
            _workoutAI = new TrackingWorkoutAIService();
            _nutritionAI = new TrackingNutritionAIService();
            var bookingAIMock = new MockBookingAIService();
            var environmentMock = new MockHostEnvironment();

            var bookingRepo = new GenericRepository<Booking, Guid>(_context);
            var ragService = new SimpleRAGService(_context);
            var healthRAG = new MemberHealthRAGService(embeddingMock, geminiMock, _context);
            var attendanceMock = new MockAttendanceSuggestionService();
            var healthIntelligence = new HealthIntelligenceService(geminiMock);
            var planningPipeline = new MockFitnessPlanningPipeline(_workoutAI, _nutritionAI);

            _chatService = new ChatService(
                geminiMock,
                _workoutAI,
                _nutritionAI,
                bookingAIMock,
                _context,
                bookingRepo,
                ragService,
                NullLogger<ChatService>.Instance,
                environmentMock,
                healthRAG,
                attendanceMock,
                healthIntelligence,
                planningPipeline
            );
        }

        public class MockFitnessPlanningPipeline : IFitnessPlanningPipeline
        {
            private readonly IWorkoutAIService _workoutAI;
            private readonly INutritionAIService _nutritionAI;

            public MockFitnessPlanningPipeline(IWorkoutAIService workoutAI, INutritionAIService nutritionAI)
            {
                _workoutAI = workoutAI;
                _nutritionAI = nutritionAI;
            }

            public async Task<PlanningResultDto> ProcessPlanningRequestAsync(Guid memberProfileId, string userMessage, string planType)
            {
                var result = new PlanningResultDto { PlanType = planType };
                if (planType == "workout" || planType == "both")
                {
                    result.WorkoutPlan = await _workoutAI.GenerateWorkoutPlanAsync(memberProfileId, userMessage);
                }
                if (planType == "nutrition" || planType == "both")
                {
                    result.NutritionPlan = await _nutritionAI.GenerateNutritionPlanAsync(memberProfileId, userMessage);
                }
                return result;
            }
        }

        public void Dispose()
        {
            _context.Dispose();
            _connection.Dispose();
        }

        [Fact]
        public async Task Scenario1_NutritionPlanConfirmation_ShouldTriggerGeneration()
        {
            // Seed a conversation with previous assistant message ending with a question
            var conversation = new ChatConversation
            {
                MemberProfileId = _memberId,
                Title = "Test Conversation",
                StartedAt = DateTime.UtcNow
            };
            _context.ChatConversations.Add(conversation);
            await _context.SaveChangesAsync();

            // Previous assistant question
            _context.ChatMessages.Add(new ChatMessage
            {
                ChatConversationId = conversation.Id,
                MessageText = "Would you like me to generate a nutrition plan?",
                Sender = SenderType.AI,
                Intent = "chat",
                SentAt = DateTime.UtcNow.AddSeconds(-5)
            });
            await _context.SaveChangesAsync();

            // User responds with "OK"
            var response = await _chatService.SendMessageAsync(_memberId, conversation.Id, "OK");
            
            // Assert intent has been mapped correctly to nutrition
            Assert.Equal("nutrition", response.Intent);

            // Assert that the tracking service received a contextualized message
            Assert.Contains("Would you like me to generate a nutrition plan?", _nutritionAI.LastUserMessage);
            Assert.Contains("OK", _nutritionAI.LastUserMessage);
        }

        [Fact]
        public async Task Scenario2_AdjustNutritionTargetsConfirmation_ShouldTriggerModification()
        {
            var conversation = new ChatConversation
            {
                MemberProfileId = _memberId,
                Title = "Test Conversation",
                StartedAt = DateTime.UtcNow
            };
            _context.ChatConversations.Add(conversation);
            await _context.SaveChangesAsync();

            _context.ChatMessages.Add(new ChatMessage
            {
                ChatConversationId = conversation.Id,
                MessageText = "Would you like me to adjust your daily nutrition targets?",
                Sender = SenderType.AI,
                Intent = "chat",
                SentAt = DateTime.UtcNow.AddSeconds(-5)
            });
            await _context.SaveChangesAsync();

            var response = await _chatService.SendMessageAsync(_memberId, conversation.Id, "Yes");
            
            Assert.Equal("MODIFY_NUTRITION_PLAN", response.Intent);
            Assert.Contains("Would you like me to adjust your daily nutrition targets?", _nutritionAI.LastUserMessage);
            Assert.Contains("Yes", _nutritionAI.LastUserMessage);
        }

        [Fact]
        public async Task Scenario3_ReplaceExercisesConfirmation_ShouldTriggerWorkoutModification()
        {
            var conversation = new ChatConversation
            {
                MemberProfileId = _memberId,
                Title = "Test Conversation",
                StartedAt = DateTime.UtcNow
            };
            _context.ChatConversations.Add(conversation);
            await _context.SaveChangesAsync();

            _context.ChatMessages.Add(new ChatMessage
            {
                ChatConversationId = conversation.Id,
                MessageText = "Would you like me to replace those exercises?",
                Sender = SenderType.AI,
                Intent = "chat",
                SentAt = DateTime.UtcNow.AddSeconds(-5)
            });
            await _context.SaveChangesAsync();

            var response = await _chatService.SendMessageAsync(_memberId, conversation.Id, "Sure.");
            
            Assert.Equal("MODIFY_WORKOUT_PLAN", response.Intent);
            Assert.Contains("Would you like me to replace those exercises?", _workoutAI.LastUserMessage);
            Assert.Contains("Sure", _workoutAI.LastUserMessage);
        }

        [Fact]
        public async Task Scenario4_ArabicNutritionPlanConfirmation_ShouldTriggerArabicGeneration()
        {
            var conversation = new ChatConversation
            {
                MemberProfileId = _memberId,
                Title = "Test Conversation",
                StartedAt = DateTime.UtcNow
            };
            _context.ChatConversations.Add(conversation);
            await _context.SaveChangesAsync();

            _context.ChatMessages.Add(new ChatMessage
            {
                ChatConversationId = conversation.Id,
                MessageText = "هل ترغب أيضاً في خطة غذائية؟",
                Sender = SenderType.AI,
                Intent = "chat",
                SentAt = DateTime.UtcNow.AddSeconds(-5)
            });
            await _context.SaveChangesAsync();

            var response = await _chatService.SendMessageAsync(_memberId, conversation.Id, "تمام");
            
            Assert.Equal("nutrition", response.Intent);
            Assert.Contains("هل ترغب أيضاً في خطة غذائية؟", _nutritionAI.LastUserMessage);
            Assert.Contains("تمام", _nutritionAI.LastUserMessage);
        }
    }

    public class MockAttendanceSuggestionService : IAttendanceSuggestionService
    {
        public Task<Result<DayOccupancyDto>> GetDayOccupancyAsync(DateTime date) => Task.FromResult(Result<DayOccupancyDto>.Success(new DayOccupancyDto()));
        public Task<Result<AttendanceSuggestionDto>> SuggestBestTimeAsync(DateTime date) => Task.FromResult(Result<AttendanceSuggestionDto>.Success(new AttendanceSuggestionDto()));
    }
}
