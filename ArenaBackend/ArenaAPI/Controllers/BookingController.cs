using ArenaApplication.Dtos.Booking;
using ArenaApplication.IServices;
using ArenaApplication.Services;
using ArenaDomain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace ArenaApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateBooking(CreateBookingDto dto)
        {
            var result = await _bookingService.CreateBooking(dto);

            if (!result.IsSuccess)
            {
                return BadRequest(result.Errors);
            }

            return Ok(result.Value);
        }

        [HttpGet]
        public async Task<IActionResult> GetUserBookings(Guid memberProfileId)
        {
            var result = await _bookingService.GetUserBookings(memberProfileId);

            if (!result.IsSuccess)
            {
                return BadRequest(result.Errors);
            }

            return Ok(result.Value);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBookingById(Guid id)
        {
            var result = await _bookingService.GetBookingById(id);

            if (!result.IsSuccess)
            {
                return BadRequest(result.Errors);
            }

            return Ok(result.Value);
        }

        [HttpPost("cancel/{id}")]
        public async Task<IActionResult> CancelBooking(Guid id)
        {
            var result = await _bookingService.CancelBooking(id);

            if (!result.IsSuccess)
            {
                return BadRequest(result.Errors);
            }

            return Ok(result.Value);
        }

        [HttpPost("reschedule/{id}")]
        public async Task<IActionResult> RescheduleBooking(Guid id, UpdateBookingDto dto)
        {
            var result = await _bookingService.RescheduleBooking(id, dto);

            if (!result.IsSuccess)
            {
                return BadRequest(result.Errors);
            }

            return Ok(result.Value);
        }

        [HttpGet("/api/admin/bookings")]
        public async Task<IActionResult> GetAdminBookings([FromQuery] BookingStatus? status, [FromQuery] DateTime? bookingDate)
        {
            var result = await _bookingService.GetAdminBookings(status, bookingDate);

            if (!result.IsSuccess)
            {
                return BadRequest(result.Errors);
            }

            return Ok(result.Value);
        }

        [HttpGet("/api/admin/bookings/today")]
        public async Task<IActionResult> GetTodaySchedule()
        {
            var result = await _bookingService.GetTodaySchedule();

            if (!result.IsSuccess)
            {
                return BadRequest(result.Errors);
            }

            return Ok(result.Value);
        }
    }
}