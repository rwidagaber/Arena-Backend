using ArenaApplication.Dtos.NotificationDtos;
using ArenaDomain.Enums;
using ArenaDomain.Interfacees;
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
        Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(Guid MemberProfileId, CancellationToken cancellationToken = default);
        Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);
        Task MarkAsReadAsync(Guid notificationId, Guid MemberProfileId, CancellationToken cancellationToken = default);
        Task MarkAllAsReadAsync(Guid MemberProfileId, CancellationToken cancellationToken = default);

       

        // ── Authentication ────────────────────────────────────────────────────
        Task NotifyWelcomeAsync(Guid userId, string firstName, CancellationToken cancellationToken = default);

        // ── Subscriptions & Payments ──────────────────────────────────────────
        Task NotifyPaymentConfirmedAsync(Guid userId, decimal amount, string planName, CancellationToken cancellationToken = default);
        Task NotifySubscriptionExpiringAsync(Guid userId, int daysLeft, CancellationToken cancellationToken = default);
        Task NotifySubscriptionExpiredAsync(Guid userId, CancellationToken cancellationToken = default);

        // ── Bookings & Attendance ─────────────────────────────────────────────
        Task NotifyBookingConfirmedAsync(Guid userId, DateTime bookingDate,CancellationToken cancellationToken = default);
        Task NotifyQrCodeGeneratedAsync(Guid userId, DateTime bookingDate, CancellationToken cancellationToken = default);
        Task NotifySessionReminderAsync(Guid userId, DateTime bookingDate, CancellationToken cancellationToken = default);
        Task NotifyAttendanceRecordedAsync(Guid userId, int remainingSessions, CancellationToken cancellationToken = default);

        // ── AI Features ───────────────────────────────────────────────────────
        Task NotifyWorkoutPlanReadyAsync(Guid userId, string planName, CancellationToken cancellationToken = default);
        Task NotifyNutritionPlanReadyAsync(Guid userId, CancellationToken cancellationToken = default);
        Task NotifyMealAnalyzedAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
