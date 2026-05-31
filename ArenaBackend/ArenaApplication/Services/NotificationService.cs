using ArenaApplication.Dtos.NotificationDtos;
using ArenaApplication.IServices;
using ArenaDomain.Entities.Notifications;
using ArenaDomain.Enums;
using ArenaDomain.Interfacees;
using ArenaInfrastructure.Repositories;
using Mapster;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
namespace ArenaApplication.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _repository;
        private readonly IEmailService _emailService;
        private readonly IUserRepository _userRepository;
        private readonly INotificationHub _notificationHub;

        public NotificationService(
            INotificationRepository repository,
            IEmailService emailService,
            IUserRepository userRepository,
            INotificationHub notificationHub)
        {
            _repository = repository;
            _emailService = emailService;
            _userRepository = userRepository;
            _notificationHub = notificationHub;

        }

        // ── Core (private) ────────────────────────────────────────────────────

        private async Task CreateAsync(
     Guid userId, string title, string message, NotificationType type,
     CancellationToken cancellationToken = default)
        {
            var entity = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                IsRead = false
            };

            await _repository.AddAsync(entity, cancellationToken);

            await _notificationHub.SendToUserAsync(
                userId,
                entity.Adapt<NotificationDto>(),
                cancellationToken);
        }

        // ── Write ─────────────────────────────────────────────────────────────

        public Task SendNotificationAsync(CreateNotificationDto dto, CancellationToken cancellationToken = default) =>
            CreateAsync(dto.UserId, dto.Title, dto.Message, dto.Type, cancellationToken);

        // ── Read ──────────────────────────────────────────────────────────────

        public async Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var list = await _repository.GetByUserIdAsync(userId, cancellationToken);
            return list.Adapt<IEnumerable<NotificationDto>>();
        }

        public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default) =>
            await _repository.GetUnreadCountAsync(userId, cancellationToken);

        public async Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default)
        {
            var notification = await _repository.GetByIdAsync(notificationId, cancellationToken);

            if (notification is null || notification.UserId != userId)
                return;

            notification.IsRead = true;
            await _repository.UpdateAsync(notification, cancellationToken);
        }

        public async Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default) =>
            await _repository.MarkAllAsReadAsync(userId, cancellationToken);

        // ── Authentication ────────────────────────────────────────────────────

        public Task NotifyWelcomeAsync(Guid userId, string firstName, CancellationToken cancellationToken = default) =>
            CreateAsync(
                userId,
                "Welcome to Arena!",
                $"Hey {firstName}! Your account is ready. Subscribe to a plan to unlock all features.",
                NotificationType.Success,
                cancellationToken);

        // ── Subscriptions & Payments ──────────────────────────────────────────

        public async Task NotifyPaymentConfirmedAsync(Guid userId, decimal amount, string planName, CancellationToken cancellationToken = default)
        {
            await CreateAsync(
                userId,
                "Payment Confirmed",
                $"Your payment of {amount:C} for the '{planName}' plan was successful. Enjoy your subscription!",
                NotificationType.Success,
                cancellationToken);

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is not null)
                await _emailService.SendPaymentConfirmedAsync(user.Email, user.FirstName, amount, planName, cancellationToken);
        }

        public async Task NotifySubscriptionExpiringAsync(Guid userId, int daysLeft, CancellationToken cancellationToken = default)
        {
            await CreateAsync(
                userId,
                "Subscription Expiring Soon",
                $"Your subscription expires in {daysLeft} day(s). Renew now to keep access to all features.",
                NotificationType.Warning,
                cancellationToken);

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is not null)
                await _emailService.SendSubscriptionExpiringAsync(user.Email, user.FirstName, daysLeft, cancellationToken);
        }

        public async Task NotifySubscriptionExpiredAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            await CreateAsync(
                userId,
                "Subscription Expired",
                "Your subscription has expired. Renew your plan to continue booking sessions and using AI features.",
                NotificationType.Error,
                cancellationToken);

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is not null)
                await _emailService.SendSubscriptionExpiredAsync(user.Email, user.FirstName, cancellationToken);
        }

        // ── Bookings & Attendance ─────────────────────────────────────────────

        public Task NotifyBookingConfirmedAsync(Guid userId, DateTime bookingDate, string? trainerName, CancellationToken cancellationToken = default)
        {
            var trainerPart = trainerName is not null ? $" with {trainerName}" : string.Empty;
            return CreateAsync(
                userId,
                "Booking Confirmed",
                $"Your gym session on {bookingDate:dddd, MMMM d 'at' h:mm tt}{trainerPart} is confirmed.",
                NotificationType.Success,
                cancellationToken);
        }

        public Task NotifyQrCodeGeneratedAsync(Guid userId, DateTime bookingDate, CancellationToken cancellationToken = default) =>
            CreateAsync(
                userId,
                "QR Code Ready",
                $"Your QR code for the session on {bookingDate:MMMM d} is ready. Show it at the gym entrance.",
                NotificationType.Info,
                cancellationToken);

        public Task NotifySessionReminderAsync(Guid userId, DateTime bookingDate, CancellationToken cancellationToken = default) =>
            CreateAsync(
                userId,
                "Session Reminder",
                $"Reminder: your gym session starts at {bookingDate:h:mm tt} today. Don't forget your QR code!",
                NotificationType.Warning,
                cancellationToken);

        public Task NotifyAttendanceRecordedAsync(Guid userId, int remainingSessions, CancellationToken cancellationToken = default) =>
            CreateAsync(
                userId,
                "Attendance Recorded",
                $"Check-in successful! You have {remainingSessions} session(s) remaining in your current plan.",
                NotificationType.Success,
                cancellationToken);

        // ── AI Features ───────────────────────────────────────────────────────

        public Task NotifyWorkoutPlanReadyAsync(Guid userId, string planName, CancellationToken cancellationToken = default) =>
            CreateAsync(
                userId,
                "Workout Plan Ready",
                $"Your AI-generated workout plan '{planName}' is ready. Head to your dashboard to get started!",
                NotificationType.Success,
                cancellationToken);

        public Task NotifyNutritionPlanReadyAsync(Guid userId, CancellationToken cancellationToken = default) =>
            CreateAsync(
                userId,
                "Nutrition Plan Ready",
                "Your personalised AI nutrition plan is ready. Check your dashboard for your daily targets.",
                NotificationType.Success,
                cancellationToken);

        public Task NotifyMealAnalyzedAsync(Guid userId, CancellationToken cancellationToken = default) =>
            CreateAsync(
                userId,
                "Meal Analysis Complete",
                "Your meal image has been analyzed. View the nutritional breakdown in your meal log.",
                NotificationType.Info,
                cancellationToken);
    }
}