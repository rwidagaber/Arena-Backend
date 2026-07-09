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
using ArenaInfrastructure.AI.Planning;
using ArenaInfrastructure.AI.Planning.Steps;
using ArenaInfrastructure.Data;
using ArenaInfrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace ArenaTests
{
    public class FitnessPlanningPipelineTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly AppDbContext _context;
        private readonly Guid _memberId;

        public FitnessPlanningPipelineTests()
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
                UserName = "planninguser",
                Email = "planning@example.com"
            };
            _context.Users.Add(appUser);

            _memberId = Guid.NewGuid();
            var profile = new MemberProfile
            {
                Id = _memberId,
                UserId = userId,
                FirstName = "PlannerMember",
                DateOfBirth = DateTime.UtcNow.AddYears(-30),
                Gender = Gender.Female,
                Goal = "Muscle Gain",
                FitnessExperience = "Intermediate",
                Equipment = "Dumbbells, Barbells",
                ActivityLevel = "Active",
                HealthProfileJson = "{\"Conditions\":[],\"Allergies\":[],\"Injuries\":[],\"Medications\":[],\"Restrictions\":[],\"PhysicalLimitations\":\"None\",\"ChronicDiseases\":\"None\",\"SleepHours\":8,\"DailySchedule\":\"9-5 Office Job\",\"PreferredWorkoutTime\":\"Evening\",\"TrainerNotes\":\"No notes\",\"Lifestyle\":\"Sedentary office\",\"FoodPreferences\":\"High protein, low carb\",\"BodyFat\":22.5,\"MuscleMass\":28.4}"
            };
            _context.MemberProfiles.Add(profile);
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Dispose();
            _connection.Dispose();
        }

        [Fact]
        public async Task Pipeline_ShouldHalt_WhenMissingInfoIsDetected()
        {
            // Arrange
            var geminiMock = new MockPlanningGeminiService(
                prefJson: "{\"goal\":\"Muscle Gain\",\"workoutFrequency\":4,\"trainingLocation\":\"Gym\",\"durationWeeks\":8,\"workoutPreferences\":\"None\",\"dietaryPreferences\":\"None\"}",
                feasibilityJson: "{\"isRealistic\":true,\"isPartiallyRealistic\":false,\"feasibilityExplanation\":\"Realistic goal.\"}",
                safetyJson: "{\"excludedExercises\":[],\"excludedFoods\":[],\"substitutions\":[]}",
                missingInfoJson: "{\"isMissingInfo\":true,\"followUpQuestions\":[\"How many days a week can you train?\"],\"clarificationMessage\":\"Coach explanation: I need your frequency.\"}"
            );

            var workoutAIMock = new MockPlanningWorkoutAIService();
            var nutritionAIMock = new MockPlanningNutritionAIService();
            var healthIntelligenceMock = new HealthIntelligenceService(geminiMock);

            var analyzeStep = new AnalyzeUserAndMessageStep(_context, geminiMock);
            var goalStep = new GoalAndTimeAssessmentStep(geminiMock);
            var safetyStep = new MedicalSafetyStep(healthIntelligenceMock, geminiMock);
            var missingStep = new MissingInfoCheckStep(geminiMock);
            var generatorStep = new PlanGeneratorStep(workoutAIMock, nutritionAIMock);
            var validatorStep = new PlanValidatorStep(healthIntelligenceMock);

            var pipeline = new FitnessPlanningPipeline(analyzeStep, goalStep, safetyStep, missingStep, generatorStep, validatorStep);

            // Act
            var result = await pipeline.ProcessPlanningRequestAsync(_memberId, "I want to start a new workout but I have chest pain", "workout");

            // Assert
            Assert.True(result.IsMissingInfo);
            Assert.Equal("Coach explanation: I need your frequency.", result.ClarificationMessage);
            Assert.Null(result.WorkoutPlan);
        }

        [Fact]
        public async Task Pipeline_ShouldGeneratePlans_WhenInfoIsComplete()
        {
            // Arrange
            var geminiMock = new MockPlanningGeminiService(
                prefJson: "{\"goal\":\"Muscle Gain\",\"workoutFrequency\":4,\"trainingLocation\":\"Gym\",\"durationWeeks\":8,\"workoutPreferences\":\"None\",\"dietaryPreferences\":\"None\"}",
                feasibilityJson: "{\"isRealistic\":true,\"isPartiallyRealistic\":false,\"feasibilityExplanation\":\"Realistic goal.\"}",
                safetyJson: "{\"excludedExercises\":[],\"excludedFoods\":[],\"substitutions\":[]}",
                missingInfoJson: "{\"isMissingInfo\":false,\"followUpQuestions\":[],\"clarificationMessage\":\"\"}"
            );

            var workoutAIMock = new MockPlanningWorkoutAIService();
            var nutritionAIMock = new MockPlanningNutritionAIService();
            var healthIntelligenceMock = new HealthIntelligenceService(geminiMock);

            var analyzeStep = new AnalyzeUserAndMessageStep(_context, geminiMock);
            var goalStep = new GoalAndTimeAssessmentStep(geminiMock);
            var safetyStep = new MedicalSafetyStep(healthIntelligenceMock, geminiMock);
            var missingStep = new MissingInfoCheckStep(geminiMock);
            var generatorStep = new PlanGeneratorStep(workoutAIMock, nutritionAIMock);
            var validatorStep = new PlanValidatorStep(healthIntelligenceMock);

            var pipeline = new FitnessPlanningPipeline(analyzeStep, goalStep, safetyStep, missingStep, generatorStep, validatorStep);

            // Act
            var result = await pipeline.ProcessPlanningRequestAsync(_memberId, "I want to build muscle, train 4 days/week at gym for 8 weeks", "both");

            // Assert
            Assert.False(result.IsMissingInfo);
            Assert.NotNull(result.WorkoutPlan);
            Assert.NotNull(result.NutritionPlan);
            Assert.Equal("Custom Workout Plan", result.WorkoutPlan.Name);
        }

        private class MockPlanningWorkoutAIService : IWorkoutAIService
        {
            public Task<WorkoutPlanDto> GenerateWorkoutPlanAsync(Guid memberProfileId, string userMessage)
                => Task.FromResult(new WorkoutPlanDto { Name = "Custom Workout Plan" });

            public Task<WorkoutPlanDto> ModifyWorkoutPlanAsync(Guid memberProfileId, string userMessage)
                => Task.FromResult(new WorkoutPlanDto { Name = "Modified Workout Plan" });
        }

        private class MockPlanningNutritionAIService : INutritionAIService
        {
            public Task<NutritionPlanResponseDto> GenerateNutritionPlanAsync(Guid memberProfileId, string userMessage)
                => Task.FromResult(new NutritionPlanResponseDto { DailyCalories = 2000 });

            public Task<NutritionPlanResponseDto> ModifyNutritionPlanAsync(Guid memberProfileId, string userMessage)
                => Task.FromResult(new NutritionPlanResponseDto { DailyCalories = 2000 });
        }

        private class MockPlanningGeminiService : IGeminiCompletionService
        {
            private readonly string _prefJson;
            private readonly string _feasibilityJson;
            private readonly string _safetyJson;
            private readonly string _missingInfoJson;

            public MockPlanningGeminiService(string prefJson, string feasibilityJson, string safetyJson, string missingInfoJson)
            {
                _prefJson = prefJson;
                _feasibilityJson = feasibilityJson;
                _safetyJson = safetyJson;
                _missingInfoJson = missingInfoJson;
            }

            public Task<string> GetCompletionAsync(string systemPrompt, List<ChatMessageDto> history, string userMessage)
            {
                var lowerUser = (userMessage ?? "").ToLowerInvariant();

                if (lowerUser.Contains("preferences"))
                {
                    return Task.FromResult(_prefJson);
                }
                if (lowerUser.Contains("feasibility"))
                {
                    return Task.FromResult(_feasibilityJson);
                }
                if (lowerUser.Contains("safety"))
                {
                    return Task.FromResult(_safetyJson);
                }
                if (lowerUser.Contains("missing") || lowerUser.Contains("enough"))
                {
                    return Task.FromResult(_missingInfoJson);
                }

                return Task.FromResult("{}");
            }

            public Task<string> GetVisionCompletionAsync(string systemPrompt, string userMessage, string imageMimeType, string imageBase64) 
                => Task.FromResult(string.Empty);

            public Task<string> TranscribeAudioAsync(string audioMimeType, string audioBase64) 
                => Task.FromResult(string.Empty);
        }
    }
}
