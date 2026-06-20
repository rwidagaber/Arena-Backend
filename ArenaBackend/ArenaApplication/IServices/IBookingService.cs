using ArenaApplication.Dtos.Booking;
using ArenaApplication.Dtos.UserSubscription;
using ArenaDomain.Enums;
using ArenaDomain.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ArenaApplication.IServices
{
    public interface IBookingService
    {
        Task<Result<BookingDto>> CreateBooking(CreateBookingDto dto);

        Task<Result<List<BookingDto>>> GetUserBookings(Guid memberProfileId);

        Task<Result<BookingDto>> GetBookingById(Guid bookingId);

        Task<Result<BookingDto>> CancelBooking(Guid bookingId);

        Task<Result<BookingDto>> RescheduleBooking(Guid bookingId, UpdateBookingDto dto);

        Task<Result<PagedResult<BookingDto>>> GetAdminBookings(BookingStatus? status, DateTime? bookingDate, int page, int pageSize);

        Task<Result<List<BookingDto>>> GetTodaySchedule();


    }
}
