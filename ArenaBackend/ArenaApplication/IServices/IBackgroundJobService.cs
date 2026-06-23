using System;
using System.Threading.Tasks;

namespace ArenaApplication.IServices
{
    public interface IBackgroundJobService
    {
        Task EnqueueEmailConfirmationAsync(Guid userId, string email, string otp);

        // =========================
        // Subscriptions
        // =========================
        Task EnqueueSubscriptionPaymentJobAsync(Guid memberId, decimal amount, string planName);
        Task ScheduleSubscriptionExpiryReminderAsync(Guid memberId, DateTime expiryDate);
        Task EnqueueSubscriptionExpiredAsync(Guid memberId);

        Task EnqueueSessionsLowAsync(Guid memberProfileId, int remainingSessions);

        // =========================
        // Bookings
        // =========================
        Task ScheduleBookingReminderAsync(Guid memberId, DateTime bookingDate);
        Task EnqueueBookingCancellationAsync(Guid memberId, DateTime bookingDate);
        Task EnqueueBookingConfirmationAsync(Guid memberId, DateTime bookingDate);
        Task EnqueueBookingRescheduledAsync(Guid memberId, DateTime newBookingDate);

        // =========================
        // (Optional Future)
        // =========================
        Task EnqueuePasswordResetTokenEmailAsync(string email, string resetToken, string userEmail); // للـ Link    
    }
}