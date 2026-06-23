using ArenaApplication.Dtos.Gym;
using ArenaDomain.Entities.Gym;
using ArenaDomain.Enums;
using ArenaDomain.Interfaces;
using Mapster;
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

        public WorkingHoursService(IGenericRepository<WorkingHours, int> repository)
        {
            _repository = repository;
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

            return entity.Adapt<WorkingHoursDto>();
        }

        public async Task BulkUpdateWorkingHoursAsync(
            IEnumerable<int> ids,
            UpdateWorkingHoursDto dto,
            CancellationToken cancellationToken = default)
        {
            var idList = ids.ToList();

            // Fetch ALL matching entities in a single SQL query.
            // FindAsync uses Where().ToListAsync(), which attaches all returned entities to the
            // EF change tracker in the Unchanged state — no tracker conflict possible here.
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

            // Mutate all tracked entities. Because FindAsync already attached them to the EF
            // change tracker, modifying their properties marks each one as Modified automatically.
            // No explicit UpdateAsync call is needed here.
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
        }
    }
}


