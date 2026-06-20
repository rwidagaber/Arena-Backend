using ArenaApplication.Dtos.Booking;
using ArenaApplication.IServices;
using ArenaApplication.Services;
using ArenaDomain.Enums;
using ArenaDomain.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System;
using System.Threading.Tasks;

namespace ArenaApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;
        private readonly IAnalyticsCacheVersionService _analyticsCacheVersionService;
        private readonly IStringLocalizer<ArenaLocalization> _localizer;

        public BookingController(
            IBookingService bookingService,
            IAnalyticsCacheVersionService analyticsCacheVersionService,
            IStringLocalizer<ArenaLocalization> localizer)
        {
            _bookingService = bookingService;
            _analyticsCacheVersionService = analyticsCacheVersionService;
            _localizer = localizer;
        }

        [HttpPost("create")]
        [Authorize(Roles = "GymMember,Admin")]
        public async Task<IActionResult> CreateBooking(CreateBookingDto dto)
        {
            var result = await _bookingService.CreateBooking(dto);

            if (!result.IsSuccess)
            {
                return BadRequest(result.Errors);
            }

            _analyticsCacheVersionService.BumpVersion();

            return Ok(result.Value);
        }

        [HttpGet]
        [Authorize(Roles = "GymMember,Admin")]
        public async Task<IActionResult> GetUserBookings([FromQuery] Guid memberProfileId)
        {
            var result = await _bookingService.GetUserBookings(memberProfileId);

            if (!result.IsSuccess)
            {
                return BadRequest(result.Errors);
            }

            _analyticsCacheVersionService.BumpVersion();

            return Ok(result.Value);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "GymMember,Admin")]
        public async Task<IActionResult> GetBookingById(Guid id)
        {
            var result = await _bookingService.GetBookingById(id);

            if (!result.IsSuccess)
            {
                return BadRequest(result.Errors);
            }

            _analyticsCacheVersionService.BumpVersion();

            return Ok(result.Value);
        }

        [HttpPost("cancel/{id}")]
        [Authorize(Roles = "GymMember,Admin")]
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
        [Authorize(Roles = "GymMember,Admin")]
        public async Task<IActionResult> RescheduleBooking(Guid id, UpdateBookingDto dto)
        {
            var result = await _bookingService.RescheduleBooking(id, dto);

            if (!result.IsSuccess)
            {
                return BadRequest(result.Errors);
            }

            return Ok(result.Value);
        }
    }
}
