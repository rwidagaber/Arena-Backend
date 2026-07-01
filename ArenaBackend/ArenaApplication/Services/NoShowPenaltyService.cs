using ArenaApplication.IServices;
using ArenaDomain.Entities.Bookings;
using ArenaDomain.Entities.Gym;
using ArenaDomain.Entities.Subscription;
using ArenaDomain.Enums;
using ArenaDomain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ArenaApplication.Services
{
    public class NoShowPenaltyService : INoShowPenaltyService
    {
        private readonly IGenericRepository<Booking, Guid> _bookingRepo;
        private readonly IGenericRepository<ArenaDomain.Entities.Subscription.UserSubscription, Guid> _subscriptionRepo;
        private readonly IGenericRepository<GymSetting, int> _settingsRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;

        public NoShowPenaltyService(
            IGenericRepository<Booking, Guid> bookingRepo,
            IGenericRepository<ArenaDomain.Entities.Subscription.UserSubscription, Guid> subscriptionRepo,
            IGenericRepository<GymSetting, int> settingsRepo,
            IUnitOfWork unitOfWork,
            INotificationService notificationService)
        {
            _bookingRepo = bookingRepo;
            _subscriptionRepo = subscriptionRepo;
            _settingsRepo = settingsRepo;
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
        }

        public async Task ProcessNoShowPenaltiesAsync(CancellationToken cancellationToken = default)
        {
            var localTime = DateTime.UtcNow.AddHours(3);

            // 1. Fetch Confirmed bookings that have passed their slot + 2 hours QR expiration window
            var confirmedBookings = await _bookingRepo.FindAsync(b =>
                b.Status == BookingStatus.Confirmed &&
                b.BookingDate.Date <= localTime.Date &&
                !b.IsDeleted,
                cancellationToken);

            var expiredBookings = confirmedBookings
                .Where(b => b.BookingDate.Date.Add(b.StartTime).Add(TimeSpan.FromHours(2)) < localTime)
                .ToList();

            if (expiredBookings.Any())
            {
                foreach (var booking in expiredBookings)
                {
                    booking.Status = BookingStatus.Expired;
                    await _bookingRepo.UpdateAsync(booking, cancellationToken);
                }
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // 2. Fetch all Expired bookings that are not yet penalized
            var unpenalizedBookings = await _bookingRepo.FindAsync(b =>
                b.Status == BookingStatus.Expired &&
                !b.NoShowPenalized &&
                !b.IsDeleted,
                cancellationToken);

            if (!unpenalizedBookings.Any())
            {
                return;
            }

            // 3. Load settings
            int threshold = 2; // Default fallback
            bool isEnabled = true; // Default fallback
            var settings = await _settingsRepo.GetAllAsync(cancellationToken);
            var setting = settings.FirstOrDefault();
            if (setting != null)
            {
                threshold = setting.NoShowThreshold;
                isEnabled = setting.IsNoShowPenaltyEnabled;
            }

            // If penalty policy is disabled/paused (e.g. during Ramadan/holidays), mark all expired bookings as penalized
            // immediately with zero deductions. This ensures members are not penalized retrospectively when it is re-enabled.
            if (!isEnabled)
            {
                foreach (var booking in unpenalizedBookings)
                {
                    booking.NoShowPenalized = true;
                    await _bookingRepo.UpdateAsync(booking, cancellationToken);
                }
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }

            // 4. Group by member profile and check if threshold is met
            var memberGroups = unpenalizedBookings.GroupBy(b => b.MemberProfileId);

            foreach (var group in memberGroups)
            {
                var memberProfileId = group.Key;
                var memberBookings = group.OrderBy(b => b.BookingDate).ThenBy(b => b.StartTime).ToList();
                int unpenalizedCount = memberBookings.Count;

                if (unpenalizedCount >= threshold)
                {
                    int deductions = unpenalizedCount / threshold;
                    int bookingsToPenalizeCount = deductions * threshold;

                    // Fetch active subscription for the member
                    var activeSubscriptions = await _subscriptionRepo.FindAsync(s =>
                        s.MemberProfileId == memberProfileId &&
                        s.Status == SubscriptionStatus.Active &&
                        s.EndDate > DateTime.UtcNow &&
                        !s.IsDeleted,
                        cancellationToken);

                    var subscription = activeSubscriptions.FirstOrDefault();

                    if (subscription != null && subscription.RemainingSessions > 0)
                    {
                        subscription.RemainingSessions = Math.Max(0, subscription.RemainingSessions - deductions);

                        if (subscription.RemainingSessions == 0)
                        {
                            subscription.Status = SubscriptionStatus.Expired;
                            await _subscriptionRepo.UpdateAsync(subscription, cancellationToken);
                            await _unitOfWork.SaveChangesAsync(cancellationToken);
                            await _notificationService.NotifySubscriptionExpiredAsync(memberProfileId, cancellationToken);
                        }
                        else
                        {
                            await _subscriptionRepo.UpdateAsync(subscription, cancellationToken);
                            await _unitOfWork.SaveChangesAsync(cancellationToken);
                            if (subscription.RemainingSessions <= 2)
                            {
                                await _notificationService.NotifySessionsExpiringSoonAsync(memberProfileId, subscription.RemainingSessions, cancellationToken);
                            }
                        }

                        // Mark bookings as penalized
                        for (int i = 0; i < bookingsToPenalizeCount; i++)
                        {
                            memberBookings[i].NoShowPenalized = true;
                            await _bookingRepo.UpdateAsync(memberBookings[i], cancellationToken);
                        }
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                    }
                }
            }
        }
    }
}
