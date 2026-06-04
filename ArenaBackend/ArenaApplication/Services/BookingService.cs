using ArenaApplication.Dtos.Booking;
using ArenaApplication.IServices;
using ArenaDomain.Entities.Bookings;
using ArenaDomain.Enums;
using ArenaDomain.Interfaces;
using ArenaDomain.Shared;
using Mapster;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Services
{
    public class BookingService : IBookingService
    {
        private readonly IGenericRepository<Booking, Guid> _bookingRepo;
        private readonly IUnitOfWork _unitOfWork;
        INotificationService _notificationService;
        private readonly IBackgroundJobService _backgroundJobService;


        public BookingService(IGenericRepository<Booking, Guid> bookingRepo, IUnitOfWork unitOfWork,INotificationService notificationService, IBackgroundJobService backgroundJobService)


        {
            _bookingRepo = bookingRepo;
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _backgroundJobService = backgroundJobService;

        }

        public async Task<Result<BookingDto>> CreateBooking(CreateBookingDto dto)
        {
            if (dto.BookingDate.Date < DateTime.UtcNow.Date)
            {
                return Result<BookingDto>.Failure("Booking date cannot be in the past");
            }

            var booking = dto.Adapt<Booking>();
            booking.Status = BookingStatus.Pending;

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
                return Result<BookingDto>.Failure("Booking not found");
            }

            return Result<BookingDto>.Success(booking.Adapt<BookingDto>());
        }

        public async Task<Result<BookingDto>> CancelBooking(Guid bookingId)
        {
            var booking = await _bookingRepo.GetByIdAsync(bookingId);

            if (booking == null)
            {
                return Result<BookingDto>.Failure("Booking not found");
            }

            if (booking.Status == BookingStatus.Cancelled)
            {
                return Result<BookingDto>.Failure("Booking is already cancelled");
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
                return Result<BookingDto>.Failure("Booking not found");
            }

            booking.BookingDate = dto.BookingDate;
            booking.StartTime = dto.StartTime;
            booking.EndTime = dto.EndTime;
            booking.Status = dto.Status;

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

        public async Task<Result<List<BookingDto>>> GetAdminBookings(BookingStatus? status, DateTime? bookingDate)
        {
            var query = _bookingRepo.GetAll();

            if (status.HasValue)
            {
                query = query.Where(b => b.Status == status.Value);
            }

            if (bookingDate.HasValue)
            {
                query = query.Where(b => b.BookingDate.Date == bookingDate.Value.Date);
            }

            var bookings = await query.ToListAsync();
            return Result<List<BookingDto>>.Success(bookings.Adapt<List<BookingDto>>());
        }

        public async Task<Result<List<BookingDto>>> GetTodaySchedule()
        {
            var today = DateTime.UtcNow.Date;
            var todayBookings = await _bookingRepo.FindAsync(b => b.BookingDate.Date == today);

            return Result<List<BookingDto>>.Success(todayBookings.Adapt<List<BookingDto>>());
        }
    }
}