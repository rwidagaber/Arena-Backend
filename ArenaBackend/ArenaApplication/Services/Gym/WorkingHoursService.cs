using ArenaApplication.Dtos.Gym;
using ArenaDomain.Entities.Gym;
using ArenaDomain.Entities.Bookings;
using ArenaDomain.Enums;
using ArenaDomain.Interfaces;
using ArenaApplication.IServices;
using Mapster;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ArenaApplication.Services.Gym
{
    public class WorkingHoursService : IWorkingHoursService
    {
        private readonly IGenericRepository<WorkingHours, int> _repository;
        private readonly IGenericRepository<Booking, Guid> _bookingRepository;
        private readonly IBackgroundJobService _backgroundJobService;
        private readonly ILogger<WorkingHoursService> _logger;

        // Custom weekday ordering starting from Saturday (0) to Friday (6)
        private static readonly Dictionary<WorkingDay, int> WeekdayOrder = new()
        {
            { WorkingDay.Saturday, 0 },
            { WorkingDay.Sunday, 1 },
            { WorkingDay.Monday, 2 },
            { WorkingDay.Tuesday, 3 },
            { WorkingDay.Wednesday, 4 },
            { WorkingDay.Thursday, 5 },
            { WorkingDay.Friday, 6 }
        };

        public WorkingHoursService(
            IGenericRepository<WorkingHours, int> repository,
            IGenericRepository<Booking, Guid> bookingRepository,
            IBackgroundJobService backgroundJobService,
            ILogger<WorkingHoursService> logger)
        {
            _repository = repository;
            _bookingRepository = bookingRepository;
            _backgroundJobService = backgroundJobService;
            _logger = logger;
        }

        public async Task<IEnumerable<WorkingHoursDto>> GetWorkingHoursAsync(CancellationToken cancellationToken = default)
        {
            var workingHours = await _repository.GetAllAsync(cancellationToken);
            return workingHours
                .Where(wh => !wh.IsDeleted)
                .OrderBy(wh => WeekdayOrder.TryGetValue(wh.DayOfWeek, out var order) ? order : int.MaxValue)
                .Adapt<IEnumerable<WorkingHoursDto>>();
        }

        public async Task<WorkingHoursDto> UpdateWorkingHoursAsync(
            int id,
            UpdateWorkingHoursDto dto,
            CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken);

            if (entity == null || entity.IsDeleted)
                throw new KeyNotFoundException($"Working hours record with id '{id}' was not found.");

            if (dto.IsClosed)
            {
                // Explicitly clear stale times when day is marked as closed
                entity.OpenTime = default;
                entity.CloseTime = default;
            }
            else
            {
                if (!dto.OpenTime.HasValue)
                     throw new ArgumentException("OpenTime is required when the gym is open (IsClosed = false).");

                if (!dto.CloseTime.HasValue)
                     throw new ArgumentException("CloseTime is required when the gym is open (IsClosed = false).");

                if (dto.OpenTime.Value < TimeSpan.Zero || dto.OpenTime.Value >= TimeSpan.FromHours(24))
                     throw new ArgumentException("OpenTime must be a valid time between 00:00:00 and 23:59:59.");

                if (dto.CloseTime.Value < TimeSpan.Zero || dto.CloseTime.Value >= TimeSpan.FromHours(24))
                     throw new ArgumentException("CloseTime must be a valid time between 00:00:00 and 23:59:59.");

                entity.OpenTime = dto.OpenTime.Value;
                entity.CloseTime = dto.CloseTime.Value;
            }

            entity.IsClosed = dto.IsClosed;
            entity.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(entity, cancellationToken);

            // Cancel any bookings affected by this weekly hour schedule modification
            await CancelAffectedBookingsAsync(entity, cancellationToken);

            return entity.Adapt<WorkingHoursDto>();
        }

        public async Task BulkUpdateWorkingHoursAsync(
            IEnumerable<int> ids,
            UpdateWorkingHoursDto dto,
            CancellationToken cancellationToken = default)
        {
            var idList = ids.ToList();

            // Fetch ALL matching entities in a single SQL query.
            var entities = await _repository.FindAsync(
                e => idList.Contains(e.Id) && !e.IsDeleted,
                cancellationToken);

            if (!entities.Any())
                throw new KeyNotFoundException("No valid working hours records found for the provided IDs.");

            // Validate once — the same rules apply to every selected day.
            if (!dto.IsClosed)
            {
                if (!dto.OpenTime.HasValue)
                    throw new ArgumentException("OpenTime is required when the gym is open.");

                if (!dto.CloseTime.HasValue)
                    throw new ArgumentException("CloseTime is required when the gym is open.");
            }

            foreach (var entity in entities)
            {
                if (dto.IsClosed)
                {
                    entity.OpenTime = default;
                    entity.CloseTime = default;
                }
                else
                {
                    entity.OpenTime = dto.OpenTime!.Value;
                    entity.CloseTime = dto.CloseTime!.Value;
                }

                entity.IsClosed = dto.IsClosed;
                entity.UpdatedAt = DateTime.UtcNow;
            }

            // Single atomic commit — all selected days are saved in one database round-trip.
            await _repository.SaveChangesAsync(cancellationToken);

            // Process and cancel affected bookings for all bulk-updated days
            foreach (var entity in entities)
            {
                await CancelAffectedBookingsAsync(entity, cancellationToken);
            }
        }

        /// <summary>
        /// Finds future bookings on the modified day of week that fall outside the new schedule range
        /// (or all if closed), cancels them, and triggers background email & in-app notifications.
        /// </summary>
        private async Task CancelAffectedBookingsAsync(WorkingHours updatedHours, CancellationToken cancellationToken)
        {
            // Map WorkingDay enum values to standard .NET DayOfWeek
            DayOfWeek targetDayOfWeek;
            switch (updatedHours.DayOfWeek)
            {
                case WorkingDay.Saturday: targetDayOfWeek = DayOfWeek.Saturday; break;
                case WorkingDay.Sunday: targetDayOfWeek = DayOfWeek.Sunday; break;
                case WorkingDay.Monday: targetDayOfWeek = DayOfWeek.Monday; break;
                case WorkingDay.Tuesday: targetDayOfWeek = DayOfWeek.Tuesday; break;
                case WorkingDay.Wednesday: targetDayOfWeek = DayOfWeek.Wednesday; break;
                case WorkingDay.Thursday: targetDayOfWeek = DayOfWeek.Thursday; break;
                case WorkingDay.Friday: targetDayOfWeek = DayOfWeek.Friday; break;
                default: return;
            }

            var localTime = DateTime.UtcNow.AddHours(3);

            // Find all active (Pending/Confirmed) bookings from today onwards
            var activeBookings = await _bookingRepository.FindAsync(b =>
                !b.IsDeleted &&
                (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Pending) &&
                b.BookingDate.Date >= localTime.Date,
                cancellationToken);

            var affectedBookings = activeBookings.Where(b =>
            {
                // Must match the updated day of the week
                if (b.BookingDate.DayOfWeek != targetDayOfWeek) return false;

                // Must be strictly in the future relative to the local time
                bool isFuture = b.BookingDate.Date > localTime.Date ||
                                (b.BookingDate.Date == localTime.Date && b.StartTime > localTime.TimeOfDay);
                if (!isFuture) return false;

                // If gym is closed, all future bookings are affected
                if (updatedHours.IsClosed) return true;

                // If gym is open, check if the booking's start time lies outside the new hours
                var start = b.StartTime;
                var open = updatedHours.OpenTime;
                var close = updatedHours.CloseTime;

                bool isWithinHours = close < open
                    ? (start >= open || start < close)
                    : (start >= open && start < close);

                return !isWithinHours;
            }).ToList();

            if (affectedBookings.Any())
            {
                foreach (var booking in affectedBookings)
                {
                    booking.Status = BookingStatus.Cancelled;
                    booking.UpdatedAt = DateTime.UtcNow;

                    // Enqueue a background job to send the in-app and email notifications
                    await _backgroundJobService.EnqueueGymHoursChangedCancellationAsync(
                        booking.MemberProfileId,
                        booking.BookingDate,
                        booking.StartTime);

                    _logger.LogInformation(
                        "Automatically cancelled booking '{BookingId}' for member '{MemberId}' due to gym working hours update on day '{Day}'.",
                        booking.Id, booking.MemberProfileId, updatedHours.DayOfWeek);
                }

                // Persist the cancelled booking states to the database
                await _bookingRepository.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
