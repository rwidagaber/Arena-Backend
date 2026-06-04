using ArenaApplication.Dtos.AttendanceDtos;
using ArenaApplication.Dtos.QrCodeDtos;
using ArenaApplication.IServices;
using ArenaDomain.Entities.Bookings;
using ArenaDomain.Entities.Subscription;
using ArenaDomain.Enums;
using ArenaDomain.Interfaces;

namespace ArenaInfrastructure.Services
{
    public class QRCodeService : IQRCodeService
    {
        private readonly IGenericRepository<QRCode, Guid> _qrRepo;
        private readonly IGenericRepository<Booking, Guid> _bookingRepo;
        private readonly IGenericRepository<Attendance, Guid> _attendanceRepo;
        private readonly IGenericRepository<UserSubscription, Guid> _subscriptionRepo;
        private readonly IUnitOfWork _unitOfWork;

        public QRCodeService(
            IGenericRepository<QRCode, Guid> qrRepo,
            IGenericRepository<Booking, Guid> bookingRepo,
            IGenericRepository<Attendance, Guid> attendanceRepo,
            IGenericRepository<UserSubscription, Guid> subscriptionRepo,
            IUnitOfWork unitOfWork)
        {
            _qrRepo = qrRepo;
            _bookingRepo = bookingRepo;
            _attendanceRepo = attendanceRepo;
            _subscriptionRepo = subscriptionRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task<QrDto> GenerateAsync(Guid bookingId)
        {
            // 1. Check booking exists
            var booking = await _bookingRepo.GetByIdAsync(bookingId);
            if (booking == null)
                throw new Exception($"Booking not found: {bookingId}");

            // 2. Check if QR already exists for this booking
            var existing = await _qrRepo.FindAsync(
                q => q.BookingId == bookingId);

            if (existing.Any())
                throw new Exception("QR code already generated for this booking");

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
            await _unitOfWork.SaveChangesAsync();

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

        public async Task<QrScanResultDto> ScanAsync(string code, Guid scannedById)
        {
            // 1. Find QR by code
            var qrList = await _qrRepo.FindAsync(q => q.Code == code);
            var qr = qrList.FirstOrDefault();

            if (qr == null)
                return new QrScanResultDto
                {
                    Message = "❌ Invalid QR code"
                };

            // 2. Check if already used
            if (qr.IsUsed)
                return new QrScanResultDto
                {
                    IsAlreadyUsed = true,
                    BookingId = qr.BookingId,
                    Message = "❌ QR code already scanned"
                };

            // 3. Check if expired
            if (qr.ExpirationTime < DateTime.UtcNow)
                return new QrScanResultDto
                {
                    IsExpired = true,
                    BookingId = qr.BookingId,
                    Message = "❌ QR code has expired"
                };

            // 4. Get booking
            var booking = await _bookingRepo.GetByIdAsync(qr.BookingId);
            if (booking == null)
                return new QrScanResultDto
                {
                    Message = "❌ Booking not found"
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

            // 8. Deduct session from subscription
            var subscriptions = await _subscriptionRepo.FindAsync(
                s => s.MemberProfileId == booking.MemberProfileId
                  && s.Status == SubscriptionStatus.Active
                  && s.EndDate > DateTime.UtcNow);

            var subscription = subscriptions.FirstOrDefault();
            if (subscription != null && subscription.RemainingSessions > 0)
            {
                subscription.RemainingSessions--;
                await _subscriptionRepo.UpdateAsync(subscription);
            }

            await _unitOfWork.SaveChangesAsync();

            return new QrScanResultDto
            {
                Message = "✅ Attendance recorded successfully",
                BookingId = qr.BookingId,
                MemberProfileId = booking.MemberProfileId
            };
        }
    }
}