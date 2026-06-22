using ArenaApplication.Dtos.Attendance;
using ArenaApplication.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace ArenaApi.Controllers
{
    [ApiController]
    [Route("api/attendance")]
    [Authorize]
    public class AttendanceSuggestionController : ControllerBase
    {
        private readonly IAttendanceSuggestionService _attendanceSuggestionService;
        private readonly IBookingValidationService _bookingValidationService;
        private readonly ICurrentUserService _currentUserService;
        // Gym runs on local time; default "today" accordingly (configurable offset).
        private readonly TimeSpan _localOffset;

        public AttendanceSuggestionController(
            IAttendanceSuggestionService attendanceSuggestionService,
            IBookingValidationService bookingValidationService,
            ICurrentUserService currentUserService,
            IConfiguration configuration)
        {
            _attendanceSuggestionService = attendanceSuggestionService;
            _bookingValidationService = bookingValidationService;
            _currentUserService = currentUserService;
            var offset = int.TryParse(configuration["GymSettings:UtcOffsetHours"], out var configured) ? configured : 3;
            _localOffset = TimeSpan.FromHours(offset);
        }

        /// <summary>AI recommendation for the least-busy time to attend on a date.</summary>
        [HttpGet("suggestion")]
        public async Task<IActionResult> GetSuggestion([FromQuery] DateTime? date = null)
        {
            var target = date ?? (DateTime.UtcNow + _localOffset);
            var result = await _attendanceSuggestionService.SuggestBestTimeAsync(target);
            if (!result.IsSuccess)
                return BadRequest(result.Errors);

            return Ok(result.Value);
        }

        /// <summary>Raw hourly occupancy profile for a date (debug/analytics).</summary>
        [HttpGet("occupancy")]
        public async Task<IActionResult> GetOccupancy([FromQuery] DateTime? date = null)
        {
            var target = date ?? (DateTime.UtcNow + _localOffset);
            var result = await _attendanceSuggestionService.GetDayOccupancyAsync(target);
            if (!result.IsSuccess)
                return BadRequest(result.Errors);

            return Ok(result.Value);
        }

        /// <summary>Pre-booking check: enforces 5+ hours between same-day sessions.</summary>
        [HttpPost("validate-spacing")]
        public async Task<IActionResult> ValidateSpacing([FromBody] ValidateBookingTimeDto dto)
        {
            var memberProfileId = _currentUserService.MemberProfileId;
            var result = await _bookingValidationService.ValidateSpacingAsync(memberProfileId, dto.Date, dto.StartTime);
            if (!result.IsSuccess)
                return BadRequest(result.Errors);

            return Ok(new { allowed = true });
        }
    }
}
