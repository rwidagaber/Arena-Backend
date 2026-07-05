using ArenaApplication.AI.ArenaApplication.AI;
using ArenaApplication.Dtos.ChatDtos;
using ArenaApplication.Dtos.HealthIntelligence;
using ArenaApplication.Dtos.Nutrition;
using ArenaApplication.Dtos.WorkoutPlan;
using ArenaApplication.IServices;
using ArenaDomain.Entities;
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
    // ── Mock Services ─────────────────────────────────────────────────────────

    public class MockWorkoutAIService : IWorkoutAIService
    {
        public Task<WorkoutPlanDto> GenerateWorkoutPlanAsync(Guid memberProfileId, string userMessage) 
            => Task.FromResult(new WorkoutPlanDto());
        public Task<WorkoutPlanDto> ModifyWorkoutPlanAsync(Guid memberProfileId, string userMessage) 
            => Task.FromResult(new WorkoutPlanDto());
    }

    public class MockNutritionAIService : INutritionAIService
    {
        public Task<NutritionPlanResponseDto> GenerateNutritionPlanAsync(Guid memberProfileId, string userMessage) 
            => Task.FromResult(new NutritionPlanResponseDto());
        public Task<NutritionPlanResponseDto> ModifyNutritionPlanAsync(Guid memberProfileId, string userMessage) 
            => Task.FromResult(new NutritionPlanResponseDto());
    }

    public class MockBookingAIService : IBookingAIService
    {
        public Task<string> HandleBookingRequestAsync(Guid memberProfileId, IntentResult intent, string userMessage, string memberName = "Member") 
            => Task.FromResult(string.Empty);
    }

    public class MockEmbeddingService : IEmbeddingService
    {
        public Task<float[]> GetEmbeddingAsync(string text) 
            => Task.FromResult(new float[768]);
    }

    public class MockHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "ArenaTests";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    public class MockGeminiService : IGeminiCompletionService
    {
        public Task<string> GetCompletionAsync(string systemPrompt, List<ChatMessageDto> history, string userMessage)
        {
            var prompt = (systemPrompt ?? "").ToLowerInvariant() + " " + (userMessage ?? "").ToLowerInvariant();

            // 1. Intent Detection Prompt
            if (prompt.Contains("intent detection") || prompt.Contains("intent"))
            {
                var lowerUser = (userMessage ?? "").ToLowerInvariant();
                if (lowerUser.Contains("diabetes") || userMessage.Contains("السكر"))
                {
                    if (lowerUser.Contains("recovered") || userMessage.Contains("خفيت"))
                    {
                        return Task.FromResult("{\"intent\": \"chat\"}");
                    }
                    return Task.FromResult("{\"intent\": \"chat\", \"healthConditions\": \"Diabetes\"}");
                }
                if (lowerUser.Contains("tell me my") || userMessage.Contains("الأمراض") || userMessage.Contains("الامراض") || userMessage.Contains("الإصابات") || userMessage.Contains("الاصابات"))
                {
                    return Task.FromResult("{\"intent\": \"RETRIEVE_HEALTH_INFORMATION\"}");
                }
                return Task.FromResult("{\"intent\": \"chat\"}");
            }

            // 2. Health Fact Extraction Prompt (from chat)
            if (prompt.Contains("extractor for a gym app"))
            {
                if (userMessage.Contains("recovered") || userMessage.Contains("خفيت"))
                {
                    return Task.FromResult("{\"hasHealthInfo\": false, \"items\": [], \"hasRecoveryInfo\": true, \"recoveredItems\": [{\"keyword\": \"Diabetes\", \"category\": \"HealthCondition\"}]}");
                }
                if (userMessage.Contains("diabetes") || userMessage.Contains("السكر"))
                {
                    return Task.FromResult("{\"hasHealthInfo\": true, \"items\": [{\"extractedInfo\": \"Member has diabetes\", \"category\": \"HealthCondition\"}], \"hasRecoveryInfo\": false, \"recoveredItems\": []}");
                }
                return Task.FromResult("{\"hasHealthInfo\": false, \"items\": [], \"hasRecoveryInfo\": false, \"recoveredItems\": []}");
            }

            // 3. Health Intelligence Profile Extraction (structured JSON)
            if (prompt.Contains("medical extraction system"))
            {
                if (userMessage.Contains("recovered") || userMessage.Contains("خفيت"))
                {
                    return Task.FromResult("{\"conditions\": [], \"allergies\": [], \"injuries\": [], \"restrictions\": [], \"medications\": []}");
                }
                if (userMessage.Contains("diabetes") || userMessage.Contains("السكر"))
                {
                    return Task.FromResult("{\"conditions\": [\"Diabetes\"], \"allergies\": [], \"injuries\": [], \"restrictions\": [], \"medications\": []}");
                }
                return Task.FromResult("{\"conditions\": [], \"allergies\": [], \"injuries\": [], \"restrictions\": [], \"medications\": []}");
            }

            // 4. Workout Plan Prompt (Must be checked BEFORE Translate/Clinical to avoid collisions)
            if (prompt.Contains("personal trainer") && prompt.Contains("durationweeks"))
            {
                return Task.FromResult(@"{
  ""name"": ""Weight Loss Plan"",
  ""nameAr"": ""خطة خسارة الوزن"",
  ""durationWeeks"": 4,
  ""weeklyFrequency"": 3,
  ""estimatedCaloriesBurnPerSession"": 400,
  ""days"": [
    {
      ""dayName"": ""Monday"",
      ""dayNameAr"": ""الاثنين"",
      ""focus"": ""Upper Body"",
      ""warmup"": ""5 min light cardio"",
      ""exercises"": [
        {
          ""name"": ""Dumbbell Bench Press"",
          ""nameAr"": ""ضغط صدر بالدامبل"",
          ""description"": ""A compound upper-body exercise that primarily develops the chest while also engaging the shoulders and triceps."",
          ""descriptionAr"": ""تمرين مركب للجزء العلوي من الجسم يطور الصدر بشكل أساسي مع إشراك الكتفين والترايسبس."",
          ""sets"": 4,
          ""reps"": 10,
          ""restSeconds"": 90,
          ""muscleGroup"": ""Chest"",
          ""muscleGroupAr"": ""الصدر"",
          ""equipment"": ""Dumbbells, Flat Bench"",
          ""equipmentAr"": ""دامبل، بنش مستو"",
          ""primaryMuscles"": [""Pectoralis Major"", ""Anterior Deltoids"", ""Triceps""],
          ""secondaryMuscles"": [""Triceps"", ""Shoulders""],
          ""instructions"": [""1. Lie flat on the bench."", ""2. Hold the dumbbells above your chest."", ""3. Lower them slowly until your elbows reach approximately 90 degrees."", ""4. Press the dumbbells upward until your arms are fully extended."", ""5. Repeat with controlled movement.""],
          ""commonMistakes"": [""Bouncing the weights."", ""Locking the elbows aggressively."", ""Arching the lower back excessively.""],
          ""safetyTips"": [""Keep your feet firmly on the floor."", ""Maintain a neutral wrist position."", ""Avoid lifting weights beyond your control.""],
          ""breathing"": ""Inhale while lowering the dumbbells. Exhale while pressing upward."",
          ""difficulty"": ""Beginner"",
          ""category"": ""Strength"",
          ""videoUrl"": ""https://www.youtube.com/watch?v=vm1G1kK34c0""
        }
      ],
      ""cooldown"": ""5 min stretching""
    }
  ]
}");
            }

            // 5. Translate Report
            if (prompt.Contains("medical translator") || (prompt.Contains("translate") && prompt.Contains("detailed health report")))
            {
                if (prompt.Contains("diabetes"))
                {
                    return Task.FromResult("العضو يعاني من مرض السكري. يرجى مراقبة مستوى السكر.");
                }
                return Task.FromResult("لا يوجد لدى العضو أي أمراض معروفة.");
            }

            // 6. Generate Health Report
            if (prompt.Contains("clinical fitness assistant") || prompt.Contains("clinical") || prompt.Contains("health report"))
            {
                if (prompt.Contains("diabetes"))
                {
                    return Task.FromResult("Member has Diabetes. Monitor blood sugar.");
                }
                return Task.FromResult("Member has no known health conditions.");
            }

            // 7. Chat System Prompt
            if (prompt.Contains("arena ai") || prompt.Contains("gym trainer"))
            {
                if (prompt.Contains("health conditions: diabetes"))
                {
                    return Task.FromResult("Based on your profile, you have Diabetes (السكري).");
                }
                return Task.FromResult("You have no known health conditions.");
            }

            // 8. Language Synchronization Prompt
            if (prompt.Contains("language synchronization layer") || prompt.Contains("synchronization"))
            {
                if (prompt.Contains("diabetes"))
                {
                    if (prompt.Contains("english"))
                    {
                        return Task.FromResult("Based on your profile, you have Diabetes.");
                    }
                    return Task.FromResult("بناءً على ملفك الشخصي، فإنك تعاني من السكري.");
                }
                if (prompt.Contains("english"))
                {
                    return Task.FromResult("You have no known health conditions.");
                }
                return Task.FromResult("لا يوجد لديك أي أمراض معروفة.");
            }

            return Task.FromResult("Mock Gemini response");
        }

        public Task<string> GetVisionCompletionAsync(string systemPrompt, string userMessage, string imageMimeType, string imageBase64) 
            => Task.FromResult(string.Empty);

        public Task<string> TranscribeAudioAsync(string audioMimeType, string audioBase64) 
            => Task.FromResult(string.Empty);
    }

    // ── Tests ──────────────────────────────────────────────────────────────────

    public class HealthMemoryTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly AppDbContext _context;
        private readonly ChatService _chatService;
        private readonly Guid _memberId;

        public HealthMemoryTests()
        {
            // 1. Setup SQLite in-memory database
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;

            _context = new AppDbContext(options);
            _context.Database.EnsureCreated();

            // Seed user and member profile
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

            // 2. Setup mock and real services
            var geminiMock = new MockGeminiService();
            var embeddingMock = new MockEmbeddingService();
            var workoutAIMock = new MockWorkoutAIService();
            var nutritionAIMock = new MockNutritionAIService();
            var bookingAIMock = new MockBookingAIService();
            var environmentMock = new MockHostEnvironment();

            var bookingRepo = new GenericRepository<Booking, Guid>(_context);
            var ragService = new SimpleRAGService(_context);
            var healthRAG = new MemberHealthRAGService(embeddingMock, geminiMock, _context);
            var attendanceMock = new MockAttendanceSuggestionService();
            var healthIntelligence = new HealthIntelligenceService(geminiMock);

            _chatService = new ChatService(
                geminiMock,
                workoutAIMock,
                nutritionAIMock,
                bookingAIMock,
                _context,
                bookingRepo,
                ragService,
                NullLogger<ChatService>.Instance,
                environmentMock,
                healthRAG,
                attendanceMock,
                healthIntelligence
            );
        }

        public void Dispose()
        {
            _context.Dispose();
            _connection.Dispose();
        }

        [Fact]
        public async Task Scenario1_StoreEnglish_RetrieveArabic()
        {
            // 1. Store in English
            var storeResponse = await _chatService.SendMessageAsync(_memberId, null, "I have diabetes");
            Assert.NotNull(storeResponse);

            // Verify it was saved language-independently (English standard medical term "Diabetes")
            var profile = await _context.MemberProfiles.FindAsync(_memberId);
            Assert.NotNull(profile);
            Assert.Contains("Diabetes", profile.HealthConditions ?? "");

            // 2. Retrieve in Arabic
            var retrieveResponse = await _chatService.SendMessageAsync(_memberId, storeResponse.ConversationId, "ممكن تقول الأمراض والإصابات اللي عندي؟");
            Assert.NotNull(retrieveResponse);

            // Verify response has Arabic translation (السكري / مرض السكري)
            Assert.Contains("السكري", retrieveResponse.Reply);
        }

        [Fact]
        public async Task Scenario2_StoreArabic_RetrieveEnglish()
        {
            // 1. Store in Arabic
            var storeResponse = await _chatService.SendMessageAsync(_memberId, null, "عندي السكر");
            Assert.NotNull(storeResponse);

            // Verify it was stored language-independently as standard English "Diabetes"
            var profile = await _context.MemberProfiles.FindAsync(_memberId);
            Assert.NotNull(profile);
            Assert.Contains("Diabetes", profile.HealthConditions ?? "");

            // 2. Retrieve in English
            var retrieveResponse = await _chatService.SendMessageAsync(_memberId, storeResponse.ConversationId, "Tell me my injuries and diseases.");
            Assert.NotNull(retrieveResponse);

            // Verify response contains English "Diabetes"
            Assert.Contains("Diabetes", retrieveResponse.Reply);
        }

        [Fact]
        public async Task Scenario3_UpdateEnglish_RetrieveArabic()
        {
            // First seed a disease in the database
            var profile = await _context.MemberProfiles.FindAsync(_memberId);
            Assert.NotNull(profile);
            profile.HealthConditions = "Diabetes";
            var hpDto = new HealthProfileDto();
            hpDto.Conditions.Add("Diabetes");
            profile.HealthProfileJson = JsonSerializer.Serialize(hpDto);
            _context.MemberProfiles.Update(profile);
            await _context.SaveChangesAsync();

            // 1. Recover in English
            var recoverResponse = await _chatService.SendMessageAsync(_memberId, null, "I recovered from diabetes");
            Assert.NotNull(recoverResponse);

            // Verify it was deleted/inactivated
            var updatedProfile = await _context.MemberProfiles.FindAsync(_memberId);
            Assert.NotNull(updatedProfile);
            Assert.DoesNotContain("Diabetes", updatedProfile.HealthConditions ?? "");

            // 2. Retrieve in Arabic
            var retrieveResponse = await _chatService.SendMessageAsync(_memberId, recoverResponse.ConversationId, "ممكن تقول الأمراض والإصابات اللي عندي؟");
            Assert.NotNull(retrieveResponse);

            // Verify it is no longer listed in the response
            Assert.DoesNotContain("السكري", retrieveResponse.Reply);
        }

        [Fact]
        public async Task Scenario4_UpdateArabic_RetrieveEnglish()
        {
            // First seed a disease in the database
            var profile = await _context.MemberProfiles.FindAsync(_memberId);
            Assert.NotNull(profile);
            profile.HealthConditions = "Diabetes";
            var hpDto = new HealthProfileDto();
            hpDto.Conditions.Add("Diabetes");
            profile.HealthProfileJson = JsonSerializer.Serialize(hpDto);
            _context.MemberProfiles.Update(profile);
            await _context.SaveChangesAsync();

            // 1. Recover in Arabic
            var recoverResponse = await _chatService.SendMessageAsync(_memberId, null, "خفيت من السكر");
            Assert.NotNull(recoverResponse);

            // Verify it was deleted/inactivated
            var updatedProfile = await _context.MemberProfiles.FindAsync(_memberId);
            Assert.NotNull(updatedProfile);
            Assert.DoesNotContain("Diabetes", updatedProfile.HealthConditions ?? "");

            // 2. Retrieve in English
            var retrieveResponse = await _chatService.SendMessageAsync(_memberId, recoverResponse.ConversationId, "Tell me my injuries and diseases.");
            Assert.NotNull(retrieveResponse);

            // Verify it is no longer listed in the response
            Assert.DoesNotContain("Diabetes", retrieveResponse.Reply);
        }
    }
}
