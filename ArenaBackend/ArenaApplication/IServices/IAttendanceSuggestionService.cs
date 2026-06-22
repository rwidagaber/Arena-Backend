using ArenaApplication.Dtos.Attendance;
using ArenaDomain.Shared;
using System;
using System.Threading.Tasks;

namespace ArenaApplication.IServices
{
    public interface IAttendanceSuggestionService
    {
        /// <summary>
        /// Builds the per-hour occupancy profile for the gym on the given date,
        /// from historical check-ins on the same weekday plus bookings already
        /// placed on that date, scoped to the day's working hours.
        /// </summary>
        Task<Result<DayOccupancyDto>> GetDayOccupancyAsync(DateTime date);

        /// <summary>
        /// Recommends the least-busy time(s) to attend on the given date using the
        /// occupancy profile and working hours. Uses the AI for the wording when
        /// available, falling back to a data-driven message when the AI is
        /// unavailable or there is no historical data yet.
        /// </summary>
        Task<Result<AttendanceSuggestionDto>> SuggestBestTimeAsync(DateTime date);
    }
}
