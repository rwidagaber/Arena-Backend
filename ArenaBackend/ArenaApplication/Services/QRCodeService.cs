using ArenaApplication.Dtos.AttendanceDtos;
using ArenaApplication.Dtos.QrCodeDtos;
using ArenaApplication.IServices;
using ArenaApplication.Services;
using ArenaDomain.Entities.Bookings;
using ArenaDomain.Entities.Subscription;
using ArenaDomain.Enums;
using ArenaDomain.Interfaces;
using ArenaInfrastructure.Repositories;
using ArenaDomain.Shared;
using Microsoft.Extensions.Localization;
using System;

namespace ArenaInfrastructure.Services
{
    public class QRCodeService : IQRCodeService
    {
        private readonly IGenericRepository<QRCode, Guid> _qrRepo;
        private readonly IGenericRepository<Booking, Guid> _bookingRepo;
        private readonly IGenericRepository<Attendance, Guid> _attendanceRepo;
        private readonly IGenericRepository<UserSubscription, Guid> _subscriptionRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStringLocalizer<ArenaLocalization> _localizer;
        private readonly INotificationService _notificationService;
        private readonly IMemberProfileRepository _memberProfileRepo;

        public QRCodeService(
            IGenericRepository<QRCode, Guid> qrRepo,
            IGenericRepository<Booking, Guid> bookingRepo,
            IGenericRepository<Attendance, Guid> attendanceRepo,
            IGenericRepository<UserSubscription, Guid> subscriptionRepo,
            IUnitOfWork unitOfWork,
            IStringLocalizer<ArenaLocalization> localizer,
            INotificationService notificationService,
            IMemberProfileRepository memberProfileRepo)
        {
            _qrRepo = qrRepo;
            _bookingRepo = bookingRepo;
            _attendanceRepo = attendanceRepo;
            _subscriptionRepo = subscriptionRepo;
            _unitOfWork = unitOfWork;
            _localizer = localizer;
            _notificationService = notificationService;
            _memberProfileRepo = memberProfileRepo;
        }

        public async Task<QrDto> GenerateAsync(Guid bookingId)
        {
            // 1. Check booking exists
            var booking = await _bookingRepo.GetByIdAsync(bookingId);
            if (booking == null)
                throw new KeyNotFoundException(_localizer["BookingNotFound"]);

            // 2. Check if QR already exists for this booking
            var existing = await _qrRepo.FindAsync(q => q.BookingId == bookingId);
            var existingQr = existing.FirstOrDefault();

            if (existingQr != null)
            {
                return new QrDto
                {
                    Id = existingQr.Id,
                    Code = existingQr.Code,
                    GeneratedAt = existingQr.GeneratedAt,
                    ExpirationTime = existingQr.ExpirationTime,
                    IsUsed = existingQr.IsUsed,
                    BookingId = existingQr.BookingId
                };
            }

            // 3. Generate unique code
            var code = $"ARENA-{bookingId.ToString().ToUpper().Substring(0, 8)}-{DateTime.UtcNow.Ticks}";

            // 4. Create QR record
            var qr = new QRCode
            {
                BookingId = bookingId,
                Code = code,
                GeneratedAt = DateTime.UtcNow,
                ExpirationTime = booking.BookingDate.Add(
                    booking.StartTime.Add(TimeSpan.FromHours(2))),
                IsUsed = false
            };

            await _qrRepo.AddAsync(qr);

            // Save the core transaction first
            await _unitOfWork.SaveChangesAsync();

            await _notificationService.NotifyQrCodeGeneratedAsync(
                booking.MemberProfileId,
                booking.BookingDate);

            return new QrDto
            {
                Id = qr.Id,
                Code = qr.Code,
                GeneratedAt = qr.GeneratedAt,
                ExpirationTime = qr.ExpirationTime,
                IsUsed = qr.IsUsed,
                BookingId = qr.BookingId
            };
        }

        public async Task<QrScanResultDto> ScanAsync(string code, Guid? scannedById)
        {
            // 1. Find QR by code
            var qrList = await _qrRepo.FindAsync(q => q.Code == code);
            var qr = qrList.FirstOrDefault();

            if (qr == null)
                return new QrScanResultDto
                {
                    Message = _localizer["QRInvalid"]
                };

            // 2. Check if already used
            if (qr.IsUsed)
                return new QrScanResultDto
                {
                    IsAlreadyUsed = true,
                    BookingId = qr.BookingId,
                    Message = _localizer["QRAlreadyScanned"]
                };

            // 3. Check if expired
            if (qr.ExpirationTime < DateTime.UtcNow)
                return new QrScanResultDto
                {
                    IsExpired = true,
                    BookingId = qr.BookingId,
                    Message = _localizer["QRExpired"]
                };

            // 4. Get booking
            var booking = await _bookingRepo.GetByIdAsync(qr.BookingId);
            if (booking == null)
                return new QrScanResultDto
                {
                    Message = _localizer["BookingNotFound"]
                };

            // 5. Mark QR as used
            qr.IsUsed = true;
            await _qrRepo.UpdateAsync(qr);

            // 6. Create Attendance record
            var attendance = new Attendance
            {
                BookingId = qr.BookingId,
                MemberProfileId = booking.MemberProfileId,
                CheckInTime = DateTime.UtcNow,
                ScannedById = scannedById
            };
            await _attendanceRepo.AddAsync(attendance);

            // 7. Update booking status
            booking.Status = BookingStatus.Completed;
            await _bookingRepo.UpdateAsync(booking);

            // 8. Deduct session from subscription (no notifications inside this block)
            var subscriptions = await _subscriptionRepo.FindAsync(
                s => s.MemberProfileId == booking.MemberProfileId
                  && s.Status == SubscriptionStatus.Active
                  && s.EndDate > DateTime.UtcNow);

            var subscription = subscriptions.FirstOrDefault();

            bool shouldNotifyExpiringSoon = false;
            bool shouldNotifyExpired = false;
            int remainingSessionsAfterDeduction = 0;

            if (subscription != null && subscription.RemainingSessions > 0)
            {
                subscription.RemainingSessions--;
                remainingSessionsAfterDeduction = subscription.RemainingSessions;

                if (subscription.RemainingSessions == 2)
                {
                    shouldNotifyExpiringSoon = true;
                }
                else if (subscription.RemainingSessions == 0)
                {
                    subscription.Status = SubscriptionStatus.Expired;
                    shouldNotifyExpired = true;
                }

                await _subscriptionRepo.UpdateAsync(subscription);
            }

            // Single save for the whole core transaction
            await _unitOfWork.SaveChangesAsync();

            if (shouldNotifyExpiringSoon)
                await _notificationService.NotifySessionsExpiringSoonAsync(
                    booking.MemberProfileId, remainingSessionsAfterDeduction);

            if (shouldNotifyExpired)
                await _notificationService.NotifySubscriptionExpiredAsync(
                    booking.MemberProfileId);

            // 9. Load member profile for result details
            var memberProfile = await _memberProfileRepo.GetByIdAsync(booking.MemberProfileId);
            var memberName = memberProfile?.User != null
                ? $"{memberProfile.User.FirstName} {memberProfile.User.LastName}".Trim()
                : null;

            return new QrScanResultDto
            {
                Message = _localizer["QRScannedSuccess"],
                BookingId = qr.BookingId,
                MemberProfileId = booking.MemberProfileId,
                MemberName = memberName,
                SessionDate = booking.BookingDate,
                StartTime = booking.StartTime,
                EndTime = booking.EndTime
            };
        }
    }
}