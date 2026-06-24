using ArenaApplication.IServices;
using ArenaDomain.Entities.Bookings;
using ArenaDomain.Entities.Gym;
using ArenaDomain.Enums;
using ArenaDomain.Interfaces;
using ArenaDomain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ArenaApplication.Services
{
    public class BookingValidationService : IBookingValidationService
    {
        /// <summary>Minimum gap required between two bookings on the same day.</summary>
        public const int MinHoursBetweenSameDayBookings = 5;

        private const int DefaultUtcOffsetHours = 3;

        private readonly IGenericRepository<Booking, Guid> _bookingRepo;
        private readonly IGenericRepository<WorkingHours, int> _workingHoursRepo;
        private readonly TimeSpan _localOffset;

        public BookingValidationService(
            IGenericRepository<Booking, Guid> bookingRepo,
            IGenericRepository<WorkingHours, int> workingHoursRepo,
            IConfiguration configuration)
        {
            _bookingRepo = bookingRepo;
            _workingHoursRepo = workingHoursRepo;
            var offset = int.TryParse(configuration["GymSettings:UtcOffsetHours"], out var configured) ? configured : DefaultUtcOffsetHours;
            _localOffset = TimeSpan.FromHours(offset);
        }

        public async Task<Result<bool>> ValidateSpacingAsync(Guid memberProfileId, DateTime date, TimeSpan startTime)
        {
            var targetDate = date.Date;

            // 1) Cannot book in the past (local gym time).
            var localNow = DateTime.UtcNow + _localOffset;
            if (targetDate + startTime < localNow)
                return Result<bool>.Failure("You can't book a session in the past.");

            // 2) The gym must be open that day, and the time within working hours.
            var workingDay = ToWorkingDay(targetDate.DayOfWeek);
            var hours = await _workingHoursRepo.GetAll()
                .FirstOrDefaultAsync(w => w.DayOfWeek == workingDay && !w.IsDeleted);

            if (hours == null || hours.IsClosed)
                return Result<bool>.Failure($"The gym is closed on {workingDay}. Please choose another day.");

            if (startTime < hours.OpenTime || startTime >= hours.CloseTime)
                return Result<bool>.Failure(
                    $"That time is outside the gym's hours ({Format(hours.OpenTime)} - {Format(hours.CloseTime)}).");

            // 3) Same-day sessions must be at least 5 hours apart.
            var sameDayStarts = await _bookingRepo.GetAll()
                .Where(b => b.MemberProfileId == memberProfileId
                         && b.BookingDate.Date == targetDate
                         && b.Status != BookingStatus.Cancelled
                         && !b.IsDeleted)
                .Select(b => b.StartTime)
                .ToListAsync();

            foreach (var existing in sameDayStarts)
            {
                var gapHours = Math.Abs((startTime - existing).TotalHours);
                if (gapHours < MinHoursBetweenSameDayBookings)
                    return Result<bool>.Failure(
                        $"You already have a session at {Format(existing)} that day. " +
                        $"Two sessions on the same day must be at least {MinHoursBetweenSameDayBookings} hours apart.");
            }

            return Result<bool>.Success(true);
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

        private static string Format(TimeSpan time)
        {
            var hour24 = time.Hours;
            var period = hour24 >= 12 ? "PM" : "AM";
            var hour12 = hour24 % 12;
            if (hour12 == 0) hour12 = 12;
            return $"{hour12}:{time.Minutes:00} {period}";
        }
    }
}
