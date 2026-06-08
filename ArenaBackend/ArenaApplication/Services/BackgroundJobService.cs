using ArenaApplication.IServices;
using Hangfire;
using System;
using System.Threading.Tasks;

namespace ArenaApplication.Services
{
    public class BackgroundJobService : IBackgroundJobService
    {
        private readonly INotificationService _notificationService;
        private readonly IBackgroundJobClient _jobClient;
        public BackgroundJobService(INotificationService notificationService, IBackgroundJobClient jobClient)
        {
            _notificationService = notificationService;
            _jobClient = jobClient;
        }

        // =========================
        // Email Confirmation
        // =========================

        public Task EnqueueEmailConfirmationAsync(Guid userId, string email, string otp)
        {
            _jobClient.Enqueue<INotificationService>(s =>
                 s.NotifyEmailConfirmationAsync(userId, email, otp, CancellationToken.None));

            return Task.CompletedTask;
        }

        // =========================
        // Password Reset
        // =========================

        public Task EnqueuePasswordResetTokenEmailAsync(string email, string resetToken, string userEmail)
        {
            _jobClient.Enqueue<INotificationService>(s =>
               s.NotifyPasswordResetAsync(email, resetToken, userEmail));

            return Task.CompletedTask;
        }

        // =========================
        // Subscriptions
        // =========================

        public Task EnqueueSubscriptionPaymentJobAsync(Guid memberId, decimal amount, string planName)
        {
            _jobClient.Enqueue(() =>
                _notificationService.NotifyPaymentConfirmedAsync(memberId, amount, planName));

            return Task.CompletedTask;
        }

        public Task ScheduleSubscriptionExpiryReminderAsync(Guid memberId, DateTime expiryDate)
        {
            var runAt = expiryDate.Date.AddDays(-5).AddHours(9);
            var delay = runAt.ToUniversalTime() - DateTime.UtcNow;

            if (delay <= TimeSpan.Zero)
                delay = TimeSpan.FromMinutes(1);

            _jobClient.Schedule(() =>
                _notificationService.NotifySubscriptionExpiringAsync(memberId, 5),
                delay);

            return Task.CompletedTask;
        }

        // =========================
        // Bookings
        // =========================

        public Task ScheduleBookingReminderAsync(Guid memberId, DateTime bookingDate)
        {
            var runAt = bookingDate.Date.AddDays(-1).AddHours(9);
            var delay = runAt.ToUniversalTime() - DateTime.UtcNow;

            if (delay <= TimeSpan.Zero)
                delay = TimeSpan.FromMinutes(1);

            _jobClient.Schedule(() =>
                _notificationService.NotifySessionReminderAsync(memberId, bookingDate),
                delay);

            return Task.CompletedTask;
        }

        public Task EnqueueBookingConfirmationAsync(Guid memberId, DateTime bookingDate)
        {
            _jobClient.Enqueue(() =>
                _notificationService.NotifyBookingConfirmedAsync(memberId, bookingDate));

            return Task.CompletedTask;
        }

        public Task EnqueueBookingCancellationAsync(Guid memberId, DateTime bookingDate)
        {
            _jobClient.Enqueue(() =>
                _notificationService.NotifyBookingCancelledAsync(memberId, bookingDate));

            return Task.CompletedTask;
        }

        public Task EnqueueBookingRescheduledAsync(Guid memberId, DateTime newBookingDate)
        {
            _jobClient.Enqueue(() =>
                _notificationService.NotifyBookingRescheduledAsync(memberId, newBookingDate));

            return Task.CompletedTask;
        }
    }
}