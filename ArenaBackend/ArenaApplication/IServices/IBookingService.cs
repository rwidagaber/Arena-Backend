using ArenaApplication.Dtos.Booking;
using ArenaDomain.Enums;
using ArenaDomain.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.IServices
{
    public interface IBookingService
    {
        Task<Result<BookingDto>> CreateBooking(CreateBookingDto dto);

        Task<Result<List<BookingDto>>> GetUserBookings(Guid memberProfileId);

        Task<Result<BookingDto>> GetBookingById(Guid bookingId);

        Task<Result<BookingDto>> CancelBooking(Guid bookingId);

        Task<Result<BookingDto>> RescheduleBooking(Guid bookingId, UpdateBookingDto dto);

        Task<Result<List<BookingDto>>> GetAdminBookings(BookingStatus? status, DateTime? bookingDate);

        Task<Result<List<BookingDto>>> GetTodaySchedule();


    }
}
