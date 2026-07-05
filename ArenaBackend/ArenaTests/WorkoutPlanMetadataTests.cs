using ArenaApplication.Dtos.WorkoutPlan;
using ArenaApplication.IServices;
using ArenaDomain.Entities;
using ArenaDomain.Entities.Subscription;
using ArenaDomain.Entities.User;
using ArenaDomain.Enums;
using ArenaApplication.Dtos.NotificationDtos;
using ArenaDomain.Interfaces;
using System.Threading;
using ArenaInfrastructure.AI;
using ArenaInfrastructure.Data;
using ArenaInfrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace ArenaTests
{
    public class WorkoutPlanMetadataTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly AppDbContext _context;
        private readonly WorkoutAIService _workoutAIService;
        private readonly Guid _memberId;

        public WorkoutPlanMetadataTests()
        {
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

            // Setup mock services
            var geminiMock = new MockGeminiService();
            var embeddingMock = new MockEmbeddingService();
            var memberRepo = new GenericRepository<MemberProfile, Guid>(_context);
            var subscriptionRepo = new GenericRepository<UserSubscription, Guid>(_context);
            var healthRAG = new MemberHealthRAGService(embeddingMock, geminiMock, _context);
            var healthIntelligence = new HealthIntelligenceService(geminiMock);
            
            // Dummy NotificationService
            var notificationService = new MockNotificationService();

            _workoutAIService = new WorkoutAIService(
                geminiMock,
                _context,
                memberRepo,
                subscriptionRepo,
                healthRAG,
                notificationService,
                healthIntelligence
            );
        }

        public void Dispose()
        {
            _context.Dispose();
            _connection.Dispose();
        }

        [Fact]
        public async Task GenerateWorkoutPlan_ShouldEnrichExercisesWithDetailedMetadata()
        {
            // 1. Generate plan
            var planDto = await _workoutAIService.GenerateWorkoutPlanAsync(_memberId, "Generate a workout plan for me");
            Assert.NotNull(planDto);

            // 2. Verify returned DTO properties
            var day = planDto.Days.FirstOrDefault();
            Assert.NotNull(day);
            var ex = day.Exercises.FirstOrDefault();
            Assert.NotNull(ex);
            var exerciseDetails = ex.Exercise;
            Assert.NotNull(exerciseDetails);

            Assert.Equal("Dumbbell Bench Press", exerciseDetails.Name);
            Assert.Contains("compound upper-body exercise", exerciseDetails.Description);
            Assert.Equal("https://www.youtube.com/watch?v=vm1G1kK34c0", exerciseDetails.VideoUrl);
            Assert.Equal("Beginner", exerciseDetails.Difficulty);
            Assert.Equal("Strength", exerciseDetails.Category);
            Assert.Contains("Inhale while lowering", exerciseDetails.Breathing);

            // Verify serialized array properties
            Assert.Contains("Pectoralis Major", exerciseDetails.PrimaryMuscles ?? "");
            Assert.Contains("Triceps", exerciseDetails.SecondaryMuscles ?? "");
            Assert.Contains("Lie flat on the bench", exerciseDetails.Instructions ?? "");
            Assert.Contains("Bouncing the weights", exerciseDetails.CommonMistakes ?? "");
            Assert.Contains("Keep your feet firmly", exerciseDetails.SafetyTips ?? "");

            // 3. Verify database storage
            var dbExercise = await _context.Exercises.FirstOrDefaultAsync(e => e.MemberProfileId == _memberId && e.Name == "Dumbbell Bench Press");
            Assert.NotNull(dbExercise);
            Assert.Equal("https://www.youtube.com/watch?v=vm1G1kK34c0", dbExercise.VideoUrl);
            Assert.Equal("Beginner", dbExercise.Difficulty);
            Assert.Equal("Strength", dbExercise.Category);
            
            // Deserialization check
            var instructions = JsonSerializer.Deserialize<List<string>>(dbExercise.Instructions ?? "[]");
            Assert.NotEmpty(instructions);
            Assert.Contains("1. Lie flat on the bench.", instructions);
        }

        [Fact]
        public async Task ModifyWorkoutPlan_ShouldKeepAndPopulateMetadata()
        {
            // 1. Modify plan
            var planDto = await _workoutAIService.ModifyWorkoutPlanAsync(_memberId, "Modify my workout to focus on arms");
            Assert.NotNull(planDto);

            // 2. Verify returned DTO has exercise details populated
            var day = planDto.Days.FirstOrDefault();
            Assert.NotNull(day);
            var ex = day.Exercises.FirstOrDefault();
            Assert.NotNull(ex);
            var exerciseDetails = ex.Exercise;
            Assert.NotNull(exerciseDetails);

            Assert.Equal("Dumbbell Bench Press", exerciseDetails.Name);
            Assert.Contains("compound", exerciseDetails.Description);
            Assert.Equal("Beginner", exerciseDetails.Difficulty);
            Assert.Equal("Strength", exerciseDetails.Category);

            // 3. Verify database storage
            var dbExercise = await _context.Exercises.FirstOrDefaultAsync(e => e.MemberProfileId == _memberId && e.Name == "Dumbbell Bench Press");
            Assert.NotNull(dbExercise);
            Assert.Equal("Beginner", dbExercise.Difficulty);
        }
    }

    // Dummy MockNotificationService
    public class MockNotificationService : INotificationService
    {
        public Task SendNotificationAsync(CreateNotificationDto dto, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(Guid memberProfileId, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<NotificationDto>>(new List<NotificationDto>());
        public Task<int> GetUnreadCountAsync(Guid memberProfileId, CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task MarkAsReadAsync(Guid notificationId, Guid memberProfileId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task MarkAllAsReadAsync(Guid memberProfileId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyEmailConfirmationAsync(Guid userId, string email, string otp, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyWelcomeAsync(Guid memberProfileId, string firstName, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyPasswordResetAsync(string email, string resetToken, string userEmail) => Task.CompletedTask;
        public Task NotifyPaymentConfirmedAsync(Guid memberProfileId, decimal amount, string planName, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifySubscriptionExpiringAsync(Guid memberProfileId, int daysLeft, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifySessionsExpiringSoonAsync(Guid memberProfileId, int remainingSessions, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifySubscriptionExpiredAsync(Guid memberProfileId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyBookingConfirmedAsync(Guid memberProfileId, DateTime bookingDate, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyBookingCancelledAsync(Guid memberProfileId, DateTime bookingDate, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyBookingRescheduledAsync(Guid memberProfileId, DateTime newBookingDate, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyQrCodeGeneratedAsync(Guid memberProfileId, DateTime bookingDate, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifySessionReminderAsync(Guid memberProfileId, DateTime bookingDate, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyAttendanceRecordedAsync(Guid memberProfileId, int remainingSessions, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyWorkoutPlanReadyAsync(Guid memberProfileId, string planName, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyNutritionPlanReadyAsync(Guid memberProfileId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyMealAnalyzedAsync(Guid memberProfileId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
