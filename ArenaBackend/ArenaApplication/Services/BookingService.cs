using ArenaApplication.Dtos.Booking;
using ArenaApplication.Dtos.UserSubscription;
using ArenaApplication.IServices;
using ArenaDomain.Entities.Bookings;
using ArenaDomain.Enums;
using ArenaDomain.Interfaces;
using ArenaDomain.Shared;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Services
{
    public class BookingService : IBookingService
    {
         private readonly IGenericRepository<Booking, Guid> _bookingRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;
        private readonly IBackgroundJobService _backgroundJobService;
        private readonly IStringLocalizer<ArenaLocalization> _localizer;


       public BookingService(
            IGenericRepository<Booking, Guid> bookingRepo,
            IUnitOfWork unitOfWork,
            INotificationService notificationService,
            IBackgroundJobService backgroundJobService,
            IStringLocalizer<ArenaLocalization> localizer)
        {
            _bookingRepo = bookingRepo;
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _backgroundJobService = backgroundJobService;
            _localizer = localizer;
        }
        public async Task<Result<BookingDto>> CreateBooking(CreateBookingDto dto)
        {
            if (dto.BookingDate.Date < DateTime.UtcNow.Date)
            {
                return Result<BookingDto>.Failure(_localizer["BookingDateCannotBeInPast"]);
            }

            var booking = dto.Adapt<Booking>();
            booking.Status = BookingStatus.Confirmed;

            await _bookingRepo.AddAsync(booking);
            await _unitOfWork.SaveChangesAsync();

            await _backgroundJobService.ScheduleBookingReminderAsync(
            booking.MemberProfileId,
            booking.BookingDate);
          

            return Result<BookingDto>.Success(booking.Adapt<BookingDto>());
        }

        public async Task<Result<List<BookingDto>>> GetUserBookings(Guid memberProfileId)
        {
            var userBookings = await _bookingRepo.FindAsync(b => b.MemberProfileId == memberProfileId);
            var result = userBookings.Adapt<List<BookingDto>>();

            return Result<List<BookingDto>>.Success(result);
        }

        public async Task<Result<BookingDto>> GetBookingById(Guid bookingId)
        {
            var booking = await _bookingRepo.GetByIdAsync(bookingId);

            if (booking == null)
            {
                return Result<BookingDto>.Failure(_localizer["BookingNotFound"]);
            }

            return Result<BookingDto>.Success(booking.Adapt<BookingDto>());
        }

        public async Task<Result<BookingDto>> CancelBooking(Guid bookingId)
        {
            var booking = await _bookingRepo.GetByIdAsync(bookingId);

            if (booking == null)
            {
                return Result<BookingDto>.Failure(_localizer["BookingNotFound"]);
            }

            if (booking.Status == BookingStatus.Cancelled)
            {
                return Result<BookingDto>.Failure(_localizer["BookingAlreadyCancelled"]);
            }

            booking.Status = BookingStatus.Cancelled;

            await _bookingRepo.UpdateAsync(booking);
            await _unitOfWork.SaveChangesAsync();

            return Result<BookingDto>.Success(booking.Adapt<BookingDto>());
        }

        public async Task<Result<BookingDto>> RescheduleBooking(Guid bookingId, UpdateBookingDto dto)
        {
            var booking = await _bookingRepo.GetByIdAsync(bookingId);

            if (booking == null)
            {
                return Result<BookingDto>.Failure(_localizer["BookingNotFound"]);
            }

            if (booking.Status == BookingStatus.Cancelled)
            {
                return Result<BookingDto>.Failure(_localizer["CancelledBookingCannotBeRescheduled"]);
            }

            if (dto.BookingDate.Date < DateTime.UtcNow.Date)
            {
                return Result<BookingDto>.Failure(_localizer["BookingDateCannotBeInPast"]);
            }

            booking.BookingDate = dto.BookingDate;
            booking.StartTime = dto.StartTime;
            booking.EndTime = dto.EndTime;
            booking.Status = BookingStatus.Confirmed;

            await _bookingRepo.UpdateAsync(booking);
            await _unitOfWork.SaveChangesAsync();

            await _backgroundJobService.ScheduleBookingReminderAsync(
            booking.MemberProfileId,
            booking.BookingDate);

            await _backgroundJobService.EnqueueBookingCancellationAsync(
               booking.MemberProfileId,
              booking.BookingDate);

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
                {
                    query = query.Where(b => b.Status == status.Value);
                }

                if (bookingDate.HasValue)
                {
                    query = query.Where(b => b.BookingDate.Date == bookingDate.Value.Date);
                }

                int totalCount = await query.CountAsync();

                var bookings = await query
                    .OrderByDescending(x => x.BookingDate)
                    .ThenBy(x => x.StartTime)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var dtos = bookings.Adapt<List<BookingDto>>();

                var pagedResult = new PagedResult<BookingDto>
                {
                    Items = dtos,
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
