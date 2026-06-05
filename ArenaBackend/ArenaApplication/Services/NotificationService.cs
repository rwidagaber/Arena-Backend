using ArenaApplication.Dtos.NotificationDtos;
using ArenaApplication.IServices;
using ArenaDomain.Entities.Notifications;
using ArenaDomain.Enums;
using ArenaDomain.Interfaces;
using ArenaDomain.Shared;
using ArenaInfrastructure.Repositories;
using Mapster;
using Microsoft.Extensions.Localization;
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
        private readonly IStringLocalizer<ArenaLocalization> _localizer;

        public NotificationService(
            INotificationRepository repository,
            IEmailService emailService,
            IMemberProfileRepository memberProfileRepository,
            INotificationHub notificationHub,
            IStringLocalizer<ArenaLocalization> localizer)
        {
            _repository = repository;
            _emailService = emailService;
            _memberProfileRepository = memberProfileRepository;
            _notificationHub = notificationHub;
            _localizer = localizer;

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
                _localizer["NotificationWelcomeTitle"],
                string.Format(_localizer["NotificationWelcomeMessage"], firstName),
                NotificationType.Success,
                cancellationToken);

        // ── Subscriptions & Payments ──────────────────────────────────────────

        public async Task NotifyPaymentConfirmedAsync(Guid MemberProfileId, decimal amount, string planName, CancellationToken cancellationToken = default)
        {
            await CreateAsync(
                MemberProfileId,
                _localizer["NotificationPaymentConfirmedTitle"],
                string.Format(_localizer["NotificationPaymentConfirmedMessage"], amount, planName),
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
                _localizer["NotificationSubscriptionExpiringTitle"],
                string.Format(_localizer["NotificationSubscriptionExpiringMessage"], daysLeft),
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
                _localizer["NotificationSubscriptionExpiredTitle"],
                _localizer["NotificationSubscriptionExpiredMessage"],
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
                _localizer["NotificationBookingConfirmedTitle"],
                string.Format(_localizer["NotificationBookingConfirmedMessage"], bookingDate),
                NotificationType.Success,   
                cancellationToken);
        }

        public Task NotifyQrCodeGeneratedAsync(Guid MemberProfileId, DateTime bookingDate, CancellationToken cancellationToken = default) =>
            CreateAsync(
                MemberProfileId,
                _localizer["NotificationQRCodeTitle"],
                string.Format(_localizer["NotificationQRCodeMessage"], bookingDate),
                NotificationType.Info,
                cancellationToken);

        public Task NotifySessionReminderAsync(Guid MemberProfileId, DateTime bookingDate, CancellationToken cancellationToken = default) =>
            CreateAsync(
                MemberProfileId,
                _localizer["NotificationSessionReminderTitle"],
                string.Format(_localizer["NotificationSessionReminderMessage"], bookingDate),
                NotificationType.Warning,
                cancellationToken);

        public Task NotifyAttendanceRecordedAsync(Guid MemberProfileId, int remainingSessions, CancellationToken cancellationToken = default) =>
            CreateAsync(
                MemberProfileId,
                _localizer["NotificationAttendanceRecordedTitle"],
                string.Format(_localizer["NotificationAttendanceRecordedMessage"], remainingSessions),
                NotificationType.Success,
                cancellationToken);

        // ── AI Features ───────────────────────────────────────────────────────

        public Task NotifyWorkoutPlanReadyAsync(Guid MemberProfileId, string planName, CancellationToken cancellationToken = default) =>
            CreateAsync(
                MemberProfileId,
                _localizer["NotificationWorkoutPlanTitle"],
                string.Format(_localizer["NotificationWorkoutPlanMessage"], planName),
                NotificationType.Success,
                cancellationToken);

        public Task NotifyNutritionPlanReadyAsync(Guid MemberProfileId, CancellationToken cancellationToken = default) =>
            CreateAsync(
                MemberProfileId,
                _localizer["NotificationNutritionPlanTitle"],
                _localizer["NotificationNutritionPlanMessage"],
                NotificationType.Success,
                cancellationToken);

        public Task NotifyMealAnalyzedAsync(Guid MemberProfileId, CancellationToken cancellationToken = default) =>
            CreateAsync(
                MemberProfileId,
                _localizer["NotificationMealAnalysisTitle"],
                _localizer["NotificationMealAnalysisMessage"],
                NotificationType.Info,
                cancellationToken);
    }
}