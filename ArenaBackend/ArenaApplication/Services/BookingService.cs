using ArenaApplication.Dtos.Booking;
using ArenaApplication.Dtos.UserSubscription;
using ArenaApplication.IServices;
using ArenaDomain.Entities.Bookings;
using ArenaDomain.Entities.Subscription;
using ArenaDomain.Entities.Gym;
using ArenaDomain.Enums;
using ArenaDomain.Interfaces;
using ArenaDomain.Shared;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArenaApplication.Services
{
    public class BookingService : IBookingService
    {
        private readonly IGenericRepository<Booking, Guid> _bookingRepo;
        private readonly IGenericRepository<ArenaDomain.Entities.Subscription.UserSubscription, Guid> _subscriptionRepo;
        private readonly IGenericRepository<WorkingHours, int> _workingHoursRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;
        private readonly IBackgroundJobService _backgroundJobService;
        private readonly IStringLocalizer<ArenaLocalization> _localizer;

        public BookingService(
            IGenericRepository<Booking, Guid> bookingRepo,
            IGenericRepository<ArenaDomain.Entities.Subscription.UserSubscription, Guid> subscriptionRepo,
            IGenericRepository<WorkingHours, int> workingHoursRepo,
            IUnitOfWork unitOfWork,
            INotificationService notificationService,
            IBackgroundJobService backgroundJobService,
            IStringLocalizer<ArenaLocalization> localizer)
        {
            _bookingRepo = bookingRepo;
            _subscriptionRepo = subscriptionRepo;
            _workingHoursRepo = workingHoursRepo;
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _backgroundJobService = backgroundJobService;
            _localizer = localizer;
        }

        public async Task<Result<BookingDto>> CreateBooking(CreateBookingDto dto)
        {
            var localTime = DateTime.UtcNow.AddHours(3);

            if (dto.BookingDate.Date < localTime.Date)
                return Result<BookingDto>.Failure(_localizer["BookingDateCannotBeInPast"]);

            if (dto.BookingDate.Date == localTime.Date && dto.StartTime <= localTime.TimeOfDay)
                return Result<BookingDto>.Failure(_localizer["BookingTimeCannotBeInPast"]);

            var subscription = (await _subscriptionRepo.FindAsync(s =>
                s.MemberProfileId == dto.MemberProfileId &&
                s.Status == SubscriptionStatus.Active &&
                s.EndDate > DateTime.UtcNow)).FirstOrDefault();

            if (subscription == null)
                return Result<BookingDto>.Failure(_localizer["ActiveSubscriptionRequired"]);

            if (subscription.RemainingSessions <= 0)
                return Result<BookingDto>.Failure(_localizer["NoRemainingSessions"]);

            var targetShiftDate = dto.BookingDate.Date;
            if (dto.StartTime < TimeSpan.FromHours(5))
                targetShiftDate = targetShiftDate.AddDays(-1);

            var dayOfWeekVal = targetShiftDate.DayOfWeek;
            var workingDayIndex = dayOfWeekVal == DayOfWeek.Sunday ? WorkingDay.Sunday : (WorkingDay)((int)dayOfWeekVal - 1);

            var workingHours = (await _workingHoursRepo.FindAsync(wh =>
                wh.DayOfWeek == workingDayIndex &&
                !wh.IsDeleted)).FirstOrDefault();

            if (workingHours == null || workingHours.IsClosed)
                return Result<BookingDto>.Failure(_localizer["GymIsClosed"]);

            var start = dto.StartTime;
            var open = workingHours.OpenTime;
            var close = workingHours.CloseTime;
            bool isWithinHours = close < open
                ? (start >= open || start < close)
                : (start >= open && start < close);

            if (!isWithinHours)
                return Result<BookingDto>.Failure(_localizer["GymIsClosed"]);

            var startDate = dto.BookingDate.Date.AddDays(-1);
            var endDate = dto.BookingDate.Date.AddDays(1);

            var candidateBookings = await _bookingRepo.FindAsync(b =>
                b.MemberProfileId == dto.MemberProfileId &&
                b.BookingDate.Date >= startDate &&
                b.BookingDate.Date <= endDate &&
                b.Status != BookingStatus.Cancelled &&
                b.Status != BookingStatus.Expired);

            var targetDateTime = dto.BookingDate.Date.Add(dto.StartTime);

            foreach (var existing in candidateBookings)
            {
                var existingDateTime = existing.BookingDate.Date.Add(existing.StartTime);
                var diff = Math.Abs((existingDateTime - targetDateTime).TotalHours);
                if (diff == 0)
                    return Result<BookingDto>.Failure(_localizer["DuplicateBooking"]);
                if (diff < 5)
                    return Result<BookingDto>.Failure(_localizer["BookingGapViolation"]);
            }

            var booking = dto.Adapt<Booking>();
            booking.Status = BookingStatus.Confirmed;
            booking.Source = dto.Source;

            await _bookingRepo.AddAsync(booking);
            await _unitOfWork.SaveChangesAsync();

            await _backgroundJobService.EnqueueBookingConfirmationAsync(
                booking.MemberProfileId,
                booking.BookingDate,
                booking.StartTime);

            await _backgroundJobService.ScheduleBookingReminderAsync(
                booking.MemberProfileId,
                booking.BookingDate,
                booking.StartTime);

            return Result<BookingDto>.Success(booking.Adapt<BookingDto>());
        }

        public async Task<Result<List<BookingDto>>> GetUserBookings(Guid memberProfileId)
        {
            var userBookings = await _bookingRepo.FindAsync(b => b.MemberProfileId == memberProfileId);
            return Result<List<BookingDto>>.Success(userBookings.Adapt<List<BookingDto>>());
        }

        public async Task<Result<BookingDto>> GetBookingById(Guid bookingId)
        {
            var booking = await _bookingRepo.GetByIdAsync(bookingId);

            if (booking == null)
                return Result<BookingDto>.Failure(_localizer["BookingNotFound"]);

            return Result<BookingDto>.Success(booking.Adapt<BookingDto>());
        }

        public async Task<Result<BookingDto>> CancelBooking(Guid bookingId)
        {
            var booking = await _bookingRepo.GetByIdAsync(bookingId);

            if (booking == null)
                return Result<BookingDto>.Failure(_localizer["BookingNotFound"]);

            if (booking.Status == BookingStatus.Cancelled)
                return Result<BookingDto>.Failure(_localizer["BookingAlreadyCancelled"]);

            booking.Status = BookingStatus.Cancelled;

            await _bookingRepo.UpdateAsync(booking);
            await _unitOfWork.SaveChangesAsync();

            await _backgroundJobService.EnqueueBookingCancellationAsync(
                booking.MemberProfileId,
                booking.BookingDate,
                booking.StartTime);

            return Result<BookingDto>.Success(booking.Adapt<BookingDto>());
        }

        public async Task<Result<BookingDto>> RescheduleBooking(Guid bookingId, UpdateBookingDto dto)
        {
            var booking = await _bookingRepo.GetByIdAsync(bookingId);

            if (booking == null)
                return Result<BookingDto>.Failure(_localizer["BookingNotFound"]);

            if (booking.Status == BookingStatus.Cancelled || booking.Status == BookingStatus.Expired)
                return Result<BookingDto>.Failure(_localizer["CancelledBookingCannotBeRescheduled"]);

            var localTime = DateTime.UtcNow.AddHours(3);

            if (dto.BookingDate.Date < localTime.Date)
                return Result<BookingDto>.Failure(_localizer["BookingDateCannotBeInPast"]);

            if (dto.BookingDate.Date == localTime.Date && dto.StartTime <= localTime.TimeOfDay)
                return Result<BookingDto>.Failure(_localizer["BookingTimeCannotBeInPast"]);

            var subscription = (await _subscriptionRepo.FindAsync(s =>
                s.MemberProfileId == booking.MemberProfileId &&
                s.Status == SubscriptionStatus.Active &&
                s.EndDate > DateTime.UtcNow)).FirstOrDefault();

            if (subscription == null)
                return Result<BookingDto>.Failure(_localizer["ActiveSubscriptionRequired"]);

            if (subscription.RemainingSessions <= 0)
                return Result<BookingDto>.Failure(_localizer["NoRemainingSessions"]);

            var targetShiftDate = dto.BookingDate.Date;
            if (dto.StartTime < TimeSpan.FromHours(5))
                targetShiftDate = targetShiftDate.AddDays(-1);

            var dayOfWeekVal = targetShiftDate.DayOfWeek;
            var workingDayIndex = dayOfWeekVal == DayOfWeek.Sunday ? WorkingDay.Sunday : (WorkingDay)((int)dayOfWeekVal - 1);

            var workingHours = (await _workingHoursRepo.FindAsync(wh =>
                wh.DayOfWeek == workingDayIndex &&
                !wh.IsDeleted)).FirstOrDefault();

            if (workingHours == null || workingHours.IsClosed)
                return Result<BookingDto>.Failure(_localizer["GymIsClosed"]);

            var start = dto.StartTime;
            var open = workingHours.OpenTime;
            var close = workingHours.CloseTime;
            bool isWithinHours = close < open
                ? (start >= open || start < close)
                : (start >= open && start < close);

            if (!isWithinHours)
                return Result<BookingDto>.Failure(_localizer["GymIsClosed"]);

            var startDate = dto.BookingDate.Date.AddDays(-1);
            var endDate = dto.BookingDate.Date.AddDays(1);

            var candidateBookings = await _bookingRepo.FindAsync(b =>
                b.MemberProfileId == booking.MemberProfileId &&
                b.BookingDate.Date >= startDate &&
                b.BookingDate.Date <= endDate &&
                b.Id != bookingId &&
                b.Status != BookingStatus.Cancelled &&
                b.Status != BookingStatus.Expired);

            var targetDateTime = dto.BookingDate.Date.Add(dto.StartTime);

            foreach (var existing in candidateBookings)
            {
                var existingDateTime = existing.BookingDate.Date.Add(existing.StartTime);
                var diff = Math.Abs((existingDateTime - targetDateTime).TotalHours);
                if (diff == 0)
                    return Result<BookingDto>.Failure(_localizer["DuplicateBooking"]);
                if (diff < 5)
                    return Result<BookingDto>.Failure(_localizer["BookingGapViolation"]);
            }

            booking.BookingDate = dto.BookingDate;
            booking.StartTime = dto.StartTime;
            booking.EndTime = dto.EndTime;
            booking.Status = BookingStatus.Confirmed;

            await _bookingRepo.UpdateAsync(booking);
            await _unitOfWork.SaveChangesAsync();

            await _backgroundJobService.EnqueueBookingRescheduledAsync(
                booking.MemberProfileId,
                booking.BookingDate,
                booking.StartTime);

            await _backgroundJobService.ScheduleBookingReminderAsync(
                booking.MemberProfileId,
                booking.BookingDate,
                booking.StartTime);

            return Result<BookingDto>.Success(booking.Adapt<BookingDto>());
        }

        public async Task<Result<PagedResult<BookingDto>>> GetAdminBookings(BookingStatus? status, DateTime? bookingDate, int page, int pageSize)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 10;

                var query = _bookingRepo.GetAll();

                if (status.HasValue)
                    query = query.Where(b => b.Status == status.Value);

                if (bookingDate.HasValue)
                    query = query.Where(b => b.BookingDate.Date == bookingDate.Value.Date);

                int totalCount = await query.CountAsync();

                var bookings = await query
                    .OrderByDescending(x => x.BookingDate)
                    .ThenBy(x => x.StartTime)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var pagedResult = new PagedResult<BookingDto>
                {
                    Items = bookings.Adapt<List<BookingDto>>(),
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                };

                return Result<PagedResult<BookingDto>>.Success(pagedResult);
            }
            catch (Exception)
            {
                return Result<PagedResult<BookingDto>>.Failure(_localizer["AnErrorOccurredWhileRetrievingBookings"]);
            }
        }

        public async Task<Result<List<BookingDto>>> GetTodaySchedule()
        {
            var today = DateTime.UtcNow.Date;
            var todayBookings = await _bookingRepo.FindAsync(b => b.BookingDate.Date == today);
            return Result<List<BookingDto>>.Success(todayBookings.Adapt<List<BookingDto>>());
        }
    }
}