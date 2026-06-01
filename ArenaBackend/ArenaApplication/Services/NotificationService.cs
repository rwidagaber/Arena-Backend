using ArenaApplication.Dtos.NotificationDtos;
using ArenaApplication.IServices;
using ArenaDomain.Entities.Notifications;
using ArenaDomain.Enums;
using ArenaDomain.Interfacees;
using ArenaInfrastructure.Repositories;
using Mapster;
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
        private readonly IMemberProfileRepository _memberProfileRepository;
        private readonly INotificationHub _notificationHub;

        public NotificationService(
            INotificationRepository repository,
            IEmailService emailService,
            IMemberProfileRepository memberProfileRepository,
            INotificationHub notificationHub)
        {
            _repository = repository;
            _emailService = emailService;
            _memberProfileRepository = memberProfileRepository;
            _notificationHub = notificationHub;

        }

        // ── Core (private) ────────────────────────────────────────────────────

        private async Task CreateAsync(
     Guid MemberProfileId, string title, string message, NotificationType type,
     CancellationToken cancellationToken = default)
        {
            var entity = new Notification
            {
                Id = Guid.NewGuid(),
                MemberProfileId = MemberProfileId,
                Title = title,
                Message = message,
                Type = type,
                IsRead = false
            };

            await _repository.AddAsync(entity, cancellationToken);

            await _notificationHub.SendToUserAsync(
                MemberProfileId,
                entity.Adapt<NotificationDto>(),
                cancellationToken);
        }

        // ── Write ─────────────────────────────────────────────────────────────

        public Task SendNotificationAsync(CreateNotificationDto dto, CancellationToken cancellationToken = default) =>
            CreateAsync(dto.MemberProfileId, dto.Title, dto.Message, dto.Type, cancellationToken);

        // ── Read ──────────────────────────────────────────────────────────────

        public async Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(Guid MemberProfileId, CancellationToken cancellationToken = default)
        {
            var list = await _repository.GetByMemberProfileIdAsync(MemberProfileId, cancellationToken);
            return list.Adapt<IEnumerable<NotificationDto>>();
        }

        public async Task<int> GetUnreadCountAsync(Guid MemberProfileId, CancellationToken cancellationToken = default) =>
            await _repository.GetUnreadCountAsync(MemberProfileId, cancellationToken);

        public async Task MarkAsReadAsync(Guid notificationId, Guid MemberProfileId, CancellationToken cancellationToken = default)
        {
            var notification = await _repository.GetByIdAsync(notificationId, cancellationToken);

            if (notification is null || notification.MemberProfileId != MemberProfileId)
                return;

            notification.IsRead = true;
            await _repository.UpdateAsync(notification, cancellationToken);
        }

        public async Task MarkAllAsReadAsync(Guid MemberProfileId, CancellationToken cancellationToken = default) =>
            await _repository.MarkAllAsReadAsync(MemberProfileId, cancellationToken);

        // ── Authentication ────────────────────────────────────────────────────

        public Task NotifyWelcomeAsync(Guid MemberProfileId, string firstName, CancellationToken cancellationToken = default) =>
            CreateAsync(
                MemberProfileId,
                "Welcome to Arena!",
                $"Hey {firstName}! Your account is ready. Subscribe to a plan to unlock all features.",
                NotificationType.Success,
                cancellationToken);

        // ── Subscriptions & Payments ──────────────────────────────────────────

        public async Task NotifyPaymentConfirmedAsync(Guid MemberProfileId, decimal amount, string planName, CancellationToken cancellationToken = default)
        {
            await CreateAsync(
                MemberProfileId,
                "Payment Confirmed",
                $"Your payment of {amount:C} for the '{planName}' plan was successful. Enjoy your subscription!",
                NotificationType.Success,
                cancellationToken);

            var user = await _memberProfileRepository.GetByIdAsync(MemberProfileId, cancellationToken);
            if (user is not null)
                await _emailService.SendPaymentConfirmedAsync(user.User.Email, user.User.FirstName, amount, planName, cancellationToken);
        }

        public async Task NotifySubscriptionExpiringAsync(Guid MemberProfileId, int daysLeft, CancellationToken cancellationToken = default)
        {
            await CreateAsync(
                MemberProfileId,
                "Subscription Expiring Soon",
                $"Your subscription expires in {daysLeft} day(s). Renew now to keep access to all features.",
                NotificationType.Warning,
                cancellationToken);

            var user = await _memberProfileRepository.GetByIdAsync(MemberProfileId, cancellationToken);
            if (user is not null)
                await _emailService.SendSubscriptionExpiringAsync(user.User.Email, user.User.FirstName, daysLeft, cancellationToken);
        }

        public async Task NotifySubscriptionExpiredAsync(Guid MemberProfileId, CancellationToken cancellationToken = default)
        {
            await CreateAsync(
                MemberProfileId,
                "Subscription Expired",
                "Your subscription has expired. Renew your plan to continue booking sessions and using AI features.",
                NotificationType.Error,
                cancellationToken);

            var user = await _memberProfileRepository.GetByIdAsync(MemberProfileId, cancellationToken);
            if (user is not null)
                await _emailService.SendSubscriptionExpiredAsync(user.User.Email, user.User.FirstName, cancellationToken);
        }

        // ── Bookings & Attendance ─────────────────────────────────────────────

        public Task NotifyBookingConfirmedAsync(Guid MemberProfileId, DateTime bookingDate, CancellationToken cancellationToken = default)
        {
            return CreateAsync(
                MemberProfileId,
                "Booking Confirmed",
                $"Your gym session on {bookingDate:dddd, MMMM d 'at' h:mm tt} is confirmed.",
                NotificationType.Success,   
                cancellationToken);
        }

        public Task NotifyQrCodeGeneratedAsync(Guid MemberProfileId, DateTime bookingDate, CancellationToken cancellationToken = default) =>
            CreateAsync(
                MemberProfileId,
                "QR Code Ready",
                $"Your QR code for the session on {bookingDate:MMMM d} is ready. Show it at the gym entrance.",
                NotificationType.Info,
                cancellationToken);

        public Task NotifySessionReminderAsync(Guid MemberProfileId, DateTime bookingDate, CancellationToken cancellationToken = default) =>
            CreateAsync(
                MemberProfileId,
                "Session Reminder",
                $"Reminder: your gym session starts at {bookingDate:h:mm tt} today. Don't forget your QR code!",
                NotificationType.Warning,
                cancellationToken);

        public Task NotifyAttendanceRecordedAsync(Guid MemberProfileId, int remainingSessions, CancellationToken cancellationToken = default) =>
            CreateAsync(
                MemberProfileId,
                "Attendance Recorded",
                $"Check-in successful! You have {remainingSessions} session(s) remaining in your current plan.",
                NotificationType.Success,
                cancellationToken);

        // ── AI Features ───────────────────────────────────────────────────────

        public Task NotifyWorkoutPlanReadyAsync(Guid MemberProfileId, string planName, CancellationToken cancellationToken = default) =>
            CreateAsync(
                MemberProfileId,
                "Workout Plan Ready",
                $"Your AI-generated workout plan '{planName}' is ready. Head to your dashboard to get started!",
                NotificationType.Success,
                cancellationToken);

        public Task NotifyNutritionPlanReadyAsync(Guid MemberProfileId, CancellationToken cancellationToken = default) =>
            CreateAsync(
                MemberProfileId,
                "Nutrition Plan Ready",
                "Your personalised AI nutrition plan is ready. Check your dashboard for your daily targets.",
                NotificationType.Success,
                cancellationToken);

        public Task NotifyMealAnalyzedAsync(Guid MemberProfileId, CancellationToken cancellationToken = default) =>
            CreateAsync(
                MemberProfileId,
                "Meal Analysis Complete",
                "Your meal image has been analyzed. View the nutritional breakdown in your meal log.",
                NotificationType.Info,
                cancellationToken);
    }
}