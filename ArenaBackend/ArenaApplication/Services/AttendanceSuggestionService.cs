using ArenaApplication.Dtos.Attendance;
using ArenaApplication.Dtos.ChatDtos;
using ArenaApplication.IServices;
using ArenaDomain.Entities.Bookings;
using ArenaDomain.Entities.Gym;
using ArenaDomain.Enums;
using ArenaDomain.Interfaces;
using ArenaDomain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArenaApplication.Services
{
    public class AttendanceSuggestionService : IAttendanceSuggestionService
    {
        // Capacity per hourly spot: more than this many bookings marks the slot full (red).
        private const int SlotCapacity = 5;

        private const int DefaultUtcOffsetHours = 3;

        private readonly IGenericRepository<Attendance, Guid> _attendanceRepo;
        private readonly IGenericRepository<Booking, Guid> _bookingRepo;
        private readonly IGenericRepository<WorkingHours, int> _workingHoursRepo;
        private readonly IGeminiCompletionService _gemini;
        // The gym runs on local wall-clock time; check-ins are stored in UTC, while
        // bookings already carry local StartTime values. Offset is configurable.
        private readonly TimeSpan _localOffset;

        public AttendanceSuggestionService(
            IGenericRepository<Attendance, Guid> attendanceRepo,
            IGenericRepository<Booking, Guid> bookingRepo,
            IGenericRepository<WorkingHours, int> workingHoursRepo,
            IGeminiCompletionService gemini,
            IConfiguration configuration)
        {
            _attendanceRepo = attendanceRepo;
            _bookingRepo = bookingRepo;
            _workingHoursRepo = workingHoursRepo;
            _gemini = gemini;
            var offset = int.TryParse(configuration["GymSettings:UtcOffsetHours"], out var configured) ? configured : DefaultUtcOffsetHours;
            _localOffset = TimeSpan.FromHours(offset);
        }

        public async Task<Result<DayOccupancyDto>> GetDayOccupancyAsync(DateTime date)
        {
            var targetDate = date.Date;
            var workingDay = ToWorkingDay(targetDate.DayOfWeek);

            var hours = await _workingHoursRepo.GetAll()
                .FirstOrDefaultAsync(w => w.DayOfWeek == workingDay && !w.IsDeleted);

            var dto = new DayOccupancyDto
            {
                Date = targetDate,
                DayOfWeek = workingDay.ToString(),
                // No configured hours is treated as closed for suggestion purposes.
                IsClosed = hours?.IsClosed ?? true,
                OpenTime = hours?.OpenTime,
                CloseTime = hours?.CloseTime
            };

            if (hours == null || hours.IsClosed)
                return Result<DayOccupancyDto>.Success(dto);

            // Historical check-ins (UTC) -> local, keep only the same weekday.
            var checkInsUtc = await _attendanceRepo.GetAll()
                .Where(a => a.CheckInTime != null && !a.IsDeleted)
                .Select(a => a.CheckInTime!.Value)
                .ToListAsync();

            var localCheckInHours = checkInsUtc
                .Select(t => DateTime.SpecifyKind(t, DateTimeKind.Utc) + _localOffset)
                .Where(t => ToWorkingDay(t.DayOfWeek) == workingDay)
                .Select(t => t.Hour)
                .ToList();

            // Bookings already placed on the target date consume capacity.
            var bookingHours = await _bookingRepo.GetAll()
                .Where(b => b.BookingDate.Date == targetDate
                         && b.Status != BookingStatus.Cancelled
                         && !b.IsDeleted)
                .Select(b => b.StartTime)
                .ToListAsync();

            // Only suggest on-the-hour slots that fall fully within the gym's DB working
            // hours: round the opening up when it has minutes (e.g. 9:30 -> first slot 10),
            // and let the last 1-hour slot end by closing time.
            var openHour = hours.OpenTime.Minutes > 0 ? hours.OpenTime.Hours + 1 : hours.OpenTime.Hours;
            var closeHour = hours.CloseTime.Hours;

            // Overnight hours (e.g. open 08:00, close 03:00 the next day): unwrap the closing
            // hour past midnight so the open window is a positive range we can iterate over.
            if (closeHour <= openHour)
                closeHour += 24;

            for (var hour = openHour; hour < closeHour; hour++)
            {
                // Hours past midnight wrap back into 0-23 for crowd matching and display.
                var slotHour = hour % 24;
                var checkInCount = localCheckInHours.Count(h => h == slotHour);
                var bookingCount = bookingHours.Count(s => s.Hours == slotHour);
                dto.Slots.Add(new OccupancySlotDto
                {
                    Hour = slotHour,
                    CheckInCount = checkInCount,
                    BookingCount = bookingCount,
                    Total = checkInCount + bookingCount,
                    OverCapacity = bookingCount > SlotCapacity
                });
            }

            var max = dto.Slots.Count > 0 ? dto.Slots.Max(s => s.Total) : 0;
            foreach (var slot in dto.Slots)
                // Over-capacity spots are always High (red); otherwise rank relative to the busiest hour.
                slot.Level = slot.OverCapacity ? "High" : ComputeLevel(slot.Total, max);

            return Result<DayOccupancyDto>.Success(dto);
        }

        public async Task<Result<AttendanceSuggestionDto>> SuggestBestTimeAsync(DateTime date)
        {
            var occupancyResult = await GetDayOccupancyAsync(date);
            if (!occupancyResult.IsSuccess)
                return Result<AttendanceSuggestionDto>.Failure(occupancyResult.Errors.ToArray());

            var occupancy = occupancyResult.Value!;
            var dto = new AttendanceSuggestionDto
            {
                Date = occupancy.Date,
                DayOfWeek = occupancy.DayOfWeek,
                IsClosed = occupancy.IsClosed,
                Occupancy = occupancy
            };

            if (occupancy.IsClosed || occupancy.Slots.Count == 0)
            {
                dto.Message = $"The gym is closed on {occupancy.DayOfWeek}. Please pick another day.";
                return Result<AttendanceSuggestionDto>.Success(dto);
            }

            // Data-driven least-busy hours (best first); also the fallback if the AI is down.
            dto.RecommendedHours = occupancy.Slots
                .OrderBy(s => s.Total)
                .ThenBy(s => s.Hour)
                .Take(3)
                .Select(s => s.Hour)
                .ToList();

            var hasHistory = occupancy.Slots.Any(s => s.Total > 0);

            try
            {
                var reply = await _gemini.GetCompletionAsync(
                    SuggestionSystemPrompt, new List<ChatMessageDto>(), BuildSuggestionPrompt(occupancy, hasHistory));

                if (!string.IsNullOrWhiteSpace(reply))
                {
                    dto.Message = reply.Trim();
                    dto.AiGenerated = true;
                    return Result<AttendanceSuggestionDto>.Success(dto);
                }
            }
            catch
            {
                // AI unavailable (no key / provider error) -> deterministic fallback below.
            }

            dto.Message = BuildFallbackMessage(occupancy, dto.RecommendedHours, hasHistory);
            return Result<AttendanceSuggestionDto>.Success(dto);
        }

        private const string SuggestionSystemPrompt =
            "You are Arena gym's scheduling assistant. Given hourly crowd levels for a day, " +
            "recommend the least crowded time(s) for a member to come in. Reply with one or two " +
            "short, friendly sentences, mention a specific time window in 12-hour format, and stay " +
            "within the gym's working hours. Do not invent data beyond what is provided.";

        private static string BuildSuggestionPrompt(DayOccupancyDto occ, bool hasHistory)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Day: {occ.DayOfWeek}");
            sb.AppendLine($"Working hours: {FormatHour(occ.OpenTime)} - {FormatHour(occ.CloseTime)}");
            sb.AppendLine(hasHistory
                ? "Hourly crowd (hour = level, total visits):"
                : "No historical crowd data yet; base the suggestion on the open hours and any existing bookings:");
            foreach (var s in occ.Slots)
                sb.AppendLine($"- {s.Hour:00}:00 = {s.Level} ({s.Total})");
            sb.AppendLine("Recommend the least crowded time to come in.");
            return sb.ToString();
        }

        private static string BuildFallbackMessage(DayOccupancyDto occ, List<int> hours, bool hasHistory)
        {
            if (hours.Count == 0)
                return $"The gym is open {FormatHour(occ.OpenTime)}-{FormatHour(occ.CloseTime)} on {occ.DayOfWeek}.";

            var best = hours[0];
            var window = $"{To12Hour(best)}-{To12Hour((best + 1) % 24)}";
            return hasHistory
                ? $"The quietest time on {occ.DayOfWeek} is around {window}. That's your best window to come in."
                : $"No crowd history yet, but {window} on {occ.DayOfWeek} is wide open - a good time to come in.";
        }

        private static string FormatHour(TimeSpan? time) => time.HasValue ? To12Hour(time.Value.Hours) : "-";

        private static string To12Hour(int hour24)
        {
            var period = hour24 >= 12 ? "PM" : "AM";
            var h = hour24 % 12;
            if (h == 0) h = 12;
            return $"{h} {period}";
        }

        private static string ComputeLevel(int total, int max)
        {
            if (max <= 0 || total == 0) return "Low";
            var ratio = (double)total / max;
            if (ratio <= 0.34) return "Low";
            if (ratio <= 0.67) return "Medium";
            return "High";
        }

        private static WorkingDay ToWorkingDay(DayOfWeek day) => day switch
        {
            DayOfWeek.Monday => WorkingDay.Monday,
            DayOfWeek.Tuesday => WorkingDay.Tuesday,
            DayOfWeek.Wednesday => WorkingDay.Wednesday,
            DayOfWeek.Thursday => WorkingDay.Thursday,
            DayOfWeek.Friday => WorkingDay.Friday,
            DayOfWeek.Saturday => WorkingDay.Saturday,
            _ => WorkingDay.Sunday
        };
    }
}
