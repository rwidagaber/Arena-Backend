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
        Task ScheduleBookingReminderAsync(Guid memberId, DateTime bookingDate, TimeSpan startTime);
        Task EnqueueBookingCancellationAsync(Guid memberId, DateTime bookingDate,TimeSpan startTime);
        Task EnqueueGymHoursChangedCancellationAsync(Guid memberId, DateTime bookingDate, TimeSpan startTime);
        Task EnqueueBookingConfirmationAsync(Guid memberId, DateTime bookingDate, TimeSpan startTime);
        Task EnqueueBookingRescheduledAsync(Guid memberId, DateTime newBookingDate, TimeSpan startTime);

        // =========================
        // (Optional Future)
        // =========================
        Task EnqueuePasswordResetTokenEmailAsync(string email, string resetToken, string userEmail); // للـ Link    
    }
}