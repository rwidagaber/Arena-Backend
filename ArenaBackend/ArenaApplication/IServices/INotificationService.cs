using ArenaApplication.Dtos.NotificationDtos;
using ArenaDomain.Enums;
using ArenaDomain.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ArenaApplication.IServices
{
    public interface INotificationService 
    {
        // ── Write ─────────────────────────────────────────────────────────────

        Task SendNotificationAsync(CreateNotificationDto dto, CancellationToken cancellationToken = default);
   
        // ── Read ──────────────────────────────────────────────────────────────
        Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(Guid memberProfileId, CancellationToken cancellationToken = default);
        Task<int> GetUnreadCountAsync(Guid memberProfileId, CancellationToken cancellationToken = default);
        Task MarkAsReadAsync(Guid notificationId, Guid memberProfileId, CancellationToken cancellationToken = default);
        Task MarkAllAsReadAsync(Guid memberProfileId, CancellationToken cancellationToken = default);



        // ── Authentication ────────────────────────────────────────────────────
        
        Task NotifyEmailConfirmationAsync(Guid userId, string email, string otp, CancellationToken cancellationToken = default);
        Task NotifyWelcomeAsync(Guid memberProfileId, string firstName, CancellationToken cancellationToken = default);

        Task NotifyPasswordResetAsync(string email, string resetToken, string userEmail);
        // ── Subscriptions & Payments ──────────────────────────────────────────
        Task NotifyPaymentConfirmedAsync(Guid memberProfileId, decimal amount, string planName, CancellationToken cancellationToken = default);
        Task NotifySubscriptionExpiringAsync(Guid memberProfileId, int daysLeft, CancellationToken cancellationToken = default);
        Task NotifySessionsExpiringSoonAsync(Guid memberProfileId, int remainingSessions, CancellationToken cancellationToken = default);

        Task NotifySubscriptionExpiredAsync(Guid memberProfileId, CancellationToken cancellationToken = default);

        // ── Bookings & Attendance ─────────────────────────────────────────────
        Task NotifyBookingConfirmedAsync(Guid memberProfileId, DateTime bookingDate, CancellationToken cancellationToken = default);
        Task NotifyBookingCancelledAsync(Guid memberProfileId, DateTime bookingDate, CancellationToken cancellationToken = default);
        Task NotifyBookingRescheduledAsync(Guid memberProfileId, DateTime newBookingDate, CancellationToken cancellationToken = default);

        Task NotifyQrCodeGeneratedAsync(Guid memberProfileId, DateTime bookingDate, CancellationToken cancellationToken = default);
        Task NotifySessionReminderAsync(Guid memberProfileId, DateTime bookingDate, CancellationToken cancellationToken = default);
        Task NotifyAttendanceRecordedAsync(Guid memberProfileId, int remainingSessions, CancellationToken cancellationToken = default);


        // ── AI Features ───────────────────────────────────────────────────────
        Task NotifyWorkoutPlanReadyAsync(Guid memberProfileId, string planName, CancellationToken cancellationToken = default);
        Task NotifyNutritionPlanReadyAsync(Guid memberProfileId, CancellationToken cancellationToken = default);
        Task NotifyMealAnalyzedAsync(Guid memberProfileId, CancellationToken cancellationToken = default);
    }
}
