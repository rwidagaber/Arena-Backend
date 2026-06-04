using ArenaApplication.Dtos.NotificationDtos;
using ArenaApplication.IServices;
using ArenaDomain.Entities.Notifications;
using ArenaDomain.Enums;
using ArenaDomain.Interfaces;
using ArenaInfrastructure.Repositories;
using Mapster;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using ArenaApplication.Dtos.NotificationDtos;
using ArenaApplication.IServices;
using ArenaDomain.Entities.Notifications;
using ArenaDomain.Enums;
using ArenaDomain.Interfaces;
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

        // =========================
        // CORE (private)
        // =========================

        private async Task CreateAsync(
            Guid memberProfileId,
            string title,
            string message,
            NotificationType type,
            CancellationToken cancellationToken = default)
        {
            var entity = new Notification
            {
                Id = Guid.NewGuid(),
                MemberProfileId = memberProfileId,
                Title = title,
                Message = message,
                Type = type,
                IsRead = false
            };

            await _repository.AddAsync(entity, cancellationToken);

            await _notificationHub.SendToUserAsync(
                memberProfileId,
                entity.Adapt<NotificationDto>(),
                cancellationToken);
        }

        // =========================
        // WRITE
        // =========================

        public Task SendNotificationAsync(CreateNotificationDto dto, CancellationToken cancellationToken = default) =>
            CreateAsync(dto.MemberProfileId, dto.Title, dto.Message, dto.Type, cancellationToken);

        // =========================
        // READ
        // =========================

        public async Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(Guid memberProfileId, CancellationToken cancellationToken = default)
        {
            var list = await _repository.GetByMemberProfileIdAsync(memberProfileId, cancellationToken);
            return list.Adapt<IEnumerable<NotificationDto>>();
        }

        public Task<int> GetUnreadCountAsync(Guid memberProfileId, CancellationToken cancellationToken = default) =>
            _repository.GetUnreadCountAsync(memberProfileId, cancellationToken);

        public async Task MarkAsReadAsync(Guid notificationId, Guid memberProfileId, CancellationToken cancellationToken = default)
        {
            var notification = await _repository.GetByIdAsync(notificationId, cancellationToken);

            if (notification is null || notification.MemberProfileId != memberProfileId)
                return;

            notification.IsRead = true;
            await _repository.UpdateAsync(notification, cancellationToken);
        }

        public Task MarkAllAsReadAsync(Guid memberProfileId, CancellationToken cancellationToken = default) =>
            _repository.MarkAllAsReadAsync(memberProfileId, cancellationToken);

        // =========================
        // AUTH
        // =========================

        // بتبعت email بس — مش بتحفظ في DB لأن الـ user مش عنده profile لسه
        public Task NotifyEmailConfirmationAsync(Guid userId, string email, string otp, CancellationToken cancellationToken = default) =>
            _emailService.SendOtpAsync(email, otp, cancellationToken);

        public Task NotifyWelcomeAsync(Guid memberProfileId, string firstName, CancellationToken cancellationToken = default) =>
            CreateAsync(
                memberProfileId,
                "Welcome to Arena!",
                $"Hey {firstName}! Your account is ready.",
                NotificationType.Success,
                cancellationToken);

        // =========================
        // SUBSCRIPTIONS & PAYMENTS
        // =========================

        public async Task NotifyPaymentConfirmedAsync(Guid memberProfileId, decimal amount, string planName, CancellationToken cancellationToken = default)
        {
            await CreateAsync(
                memberProfileId,
                "Payment Confirmed",
                $"Payment of {amount:C} for '{planName}' successful.",
                NotificationType.Success,
                cancellationToken);

            var profile = await _memberProfileRepository.GetByIdAsync(memberProfileId, cancellationToken);
            if (profile?.User != null)
                await _emailService.SendPaymentConfirmedAsync(
                    profile.User.Email!, profile.User.FirstName, amount, planName, cancellationToken);
        }

        public async Task NotifySubscriptionExpiringAsync(Guid memberProfileId, int daysLeft, CancellationToken cancellationToken = default)
        {
            await CreateAsync(
                memberProfileId,
                "Subscription Expiring Soon",
                $"Expires in {daysLeft} day(s).",
                NotificationType.Warning,
                cancellationToken);

            var profile = await _memberProfileRepository.GetByIdAsync(memberProfileId, cancellationToken);
            if (profile?.User != null)
                await _emailService.SendSubscriptionExpiringAsync(
                    profile.User.Email!, profile.User.FirstName, daysLeft, cancellationToken);
        }

        public async Task NotifySubscriptionExpiredAsync(Guid memberProfileId, CancellationToken cancellationToken = default)
        {
            await CreateAsync(
                memberProfileId,
                "Subscription Expired",
                "Your subscription has expired.",
                NotificationType.Error,
                cancellationToken);

            var profile = await _memberProfileRepository.GetByIdAsync(memberProfileId, cancellationToken);
            if (profile?.User != null)
                await _emailService.SendSubscriptionExpiredAsync(
                    profile.User.Email!, profile.User.FirstName, cancellationToken);
        }

        // =========================
        // BOOKINGS & ATTENDANCE
        // =========================

        public Task NotifyBookingConfirmedAsync(Guid memberProfileId, DateTime bookingDate, CancellationToken cancellationToken = default) =>
            CreateAsync(
                memberProfileId,
                "Booking Confirmed",
                $"Session on {bookingDate:dddd, MMMM d 'at' h:mm tt} confirmed.",
                NotificationType.Success,
                cancellationToken);

        public Task NotifyBookingCancelledAsync(Guid memberProfileId, DateTime bookingDate, CancellationToken cancellationToken = default) =>
            CreateAsync(
                memberProfileId,
                "Booking Cancelled",
                $"Session on {bookingDate:dddd, MMMM d 'at' h:mm tt} cancelled.",
                NotificationType.Warning,
                cancellationToken);

        public Task NotifyBookingRescheduledAsync(Guid memberProfileId, DateTime newBookingDate, CancellationToken cancellationToken = default) =>
            CreateAsync(
                memberProfileId,
                "Booking Rescheduled",
                $"Rescheduled to {newBookingDate:dddd, MMMM d 'at' h:mm tt}.",
                NotificationType.Info,
                cancellationToken);

        public Task NotifyQrCodeGeneratedAsync(Guid memberProfileId, DateTime bookingDate, CancellationToken cancellationToken = default) =>
            CreateAsync(
                memberProfileId,
                "QR Code Ready",
                $"QR for {bookingDate:MMMM d} ready.",
                NotificationType.Info,
                cancellationToken);

        public Task NotifySessionReminderAsync(Guid memberProfileId, DateTime bookingDate, CancellationToken cancellationToken = default) =>
            CreateAsync(
                memberProfileId,
                "Session Reminder",
                $"Session starts at {bookingDate:h:mm tt}.",
                NotificationType.Warning,
                cancellationToken);

        public Task NotifyAttendanceRecordedAsync(Guid memberProfileId, int remainingSessions, CancellationToken cancellationToken = default) =>
            CreateAsync(
                memberProfileId,
                "Attendance Recorded",
                $"{remainingSessions} sessions left.",
                NotificationType.Success,
                cancellationToken);

        // =========================
        // AI
        // =========================

        public Task NotifyWorkoutPlanReadyAsync(Guid memberProfileId, string planName, CancellationToken cancellationToken = default) =>
            CreateAsync(
                memberProfileId,
                "Workout Plan Ready",
                $"Plan '{planName}' ready.",
                NotificationType.Success,
                cancellationToken);

        public Task NotifyNutritionPlanReadyAsync(Guid memberProfileId, CancellationToken cancellationToken = default) =>
            CreateAsync(
                memberProfileId,
                "Nutrition Plan Ready",
                "Your plan is ready.",
                NotificationType.Success,
                cancellationToken);

        public Task NotifyMealAnalyzedAsync(Guid memberProfileId, CancellationToken cancellationToken = default) =>
            CreateAsync(
                memberProfileId,
                "Meal Analyzed",
                "Analysis completed.",
                NotificationType.Info,
                cancellationToken);
    }
}