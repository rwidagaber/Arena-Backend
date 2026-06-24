using ArenaApplication.IServices;
using ArenaApplication.IServices.User;
using ArenaInfrastructure.Repositories;
using Hangfire;

namespace ArenaApplication.Services
{
    public class BackgroundJobService : IBackgroundJobService
    {
        private readonly IBackgroundJobClient _jobClient;

        public BackgroundJobService(IBackgroundJobClient jobClient)
        {
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
            _jobClient.Enqueue<INotificationService>(s =>
                s.NotifyPaymentConfirmedAsync(memberId, amount, planName, CancellationToken.None));
            return Task.CompletedTask;
        }

        public Task ScheduleSubscriptionExpiryReminderAsync(Guid memberId, DateTime expiryDate)
        {
            var expiryUtc = DateTime.SpecifyKind(expiryDate, DateTimeKind.Utc);

            var runAt = expiryUtc.Date.AddDays(-3).AddHours(9);
            var delay = runAt - DateTime.UtcNow;

            if (delay <= TimeSpan.Zero) delay = TimeSpan.FromSeconds(5);

            _jobClient.Schedule<INotificationService>(s =>
                s.NotifySubscriptionExpiringAsync(memberId, 3, CancellationToken.None),
                delay);
            return Task.CompletedTask;
        }

        public Task EnqueueSubscriptionExpiredAsync(Guid memberId)
        {
            _jobClient.Enqueue<INotificationService>(s =>
                s.NotifySubscriptionExpiredAsync(memberId, CancellationToken.None));
            return Task.CompletedTask;
        }

        public Task EnqueueSessionsLowAsync(Guid memberProfileId, int remainingSessions)
        {
            _jobClient.Enqueue<INotificationService>(s =>
                s.NotifySessionsExpiringSoonAsync(memberProfileId, remainingSessions, CancellationToken.None));
            return Task.CompletedTask;
        }

        // =========================
        // Bookings
        // =========================

        public Task EnqueueBookingConfirmationAsync(Guid memberId, DateTime bookingDate, TimeSpan startTime)
        {
            var fullDateTime = DateTime.SpecifyKind(bookingDate.Date.Add(startTime), DateTimeKind.Utc);
            _jobClient.Enqueue<INotificationService>(s =>
                s.NotifyBookingConfirmedAsync(memberId, fullDateTime, CancellationToken.None));
            return Task.CompletedTask;
        }

        public Task EnqueueBookingCancellationAsync(Guid memberId, DateTime bookingDate, TimeSpan startTime)
        {
            var fullDateTime = DateTime.SpecifyKind(bookingDate.Date.Add(startTime), DateTimeKind.Utc);
            _jobClient.Enqueue<INotificationService>(s =>
                s.NotifyBookingCancelledAsync(memberId, fullDateTime, CancellationToken.None));
            return Task.CompletedTask;
        }

        public Task EnqueueBookingRescheduledAsync(Guid memberId, DateTime newBookingDate, TimeSpan startTime)
        {
            var fullDateTime = DateTime.SpecifyKind(newBookingDate.Date.Add(startTime), DateTimeKind.Utc);
            _jobClient.Enqueue<INotificationService>(s =>
                s.NotifyBookingRescheduledAsync(memberId, fullDateTime, CancellationToken.None));
            return Task.CompletedTask;
        }

        public Task ScheduleBookingReminderAsync(Guid memberId, DateTime bookingDate, TimeSpan startTime)
        {
            var fullDateTime = DateTime.SpecifyKind(bookingDate.Date.Add(startTime), DateTimeKind.Utc);
            _jobClient.Enqueue<INotificationService>(s =>
                s.NotifySessionReminderAsync(memberId, fullDateTime, CancellationToken.None));
            return Task.CompletedTask;
        }
    }
}