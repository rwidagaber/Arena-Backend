using ArenaApplication.Dtos.Workout;
using ArenaApplication.Dtos.UserSubscription;
using ArenaApplication.IServices;
using ArenaDomain.Entities.Workout;
using ArenaDomain.Entities.Gym;
using ArenaDomain.Interfaces;
using ArenaDomain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArenaApplication.Services.Gym
{
    public class ExerciseCatalogService : IExerciseCatalogService
    {
        private readonly IGenericRepository<ExerciseCatalogItem, Guid> _exerciseCatalogRepository;
        private readonly IGenericRepository<ExerciseEquipmentRequirement, Guid> _requirementRepository;
        private readonly IGenericRepository<Equipment, Guid> _equipmentRepository;
        private readonly IStringLocalizer<ArenaLocalization> _localizer;

        public ExerciseCatalogService(
            IGenericRepository<ExerciseCatalogItem, Guid> exerciseCatalogRepository,
            IGenericRepository<ExerciseEquipmentRequirement, Guid> requirementRepository,
            IGenericRepository<Equipment, Guid> equipmentRepository,
            IStringLocalizer<ArenaLocalization> localizer)
        {
            _exerciseCatalogRepository = exerciseCatalogRepository;
            _requirementRepository = requirementRepository;
            _equipmentRepository = equipmentRepository;
            _localizer = localizer;
        }

        public async Task<Result<PagedResult<ExerciseCatalogItemDto>>> GetAllAsync(string? search, int page, int pageSize)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 10;

                var query = _exerciseCatalogRepository.GetAll()
                    .AsNoTracking()
                    .Include(x => x.EquipmentRequirements)
                        .ThenInclude(r => r.Equipment);

                IQueryable<ExerciseCatalogItem> filteredQuery = query;

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var cleanSearch = search.Trim().ToLower();
                    filteredQuery = filteredQuery.Where(e =>
                        e.Name.ToLower().Contains(cleanSearch) ||
                        e.MuscleGroup.ToLower().Contains(cleanSearch) ||
                        e.DifficultyLevel.ToLower().Contains(cleanSearch));
                }

                int totalCount = await filteredQuery.CountAsync();

                var items = await filteredQuery
                    .OrderBy(e => e.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var dtos = items.Select(e => new ExerciseCatalogItemDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    Description = e.Description,
                    MuscleGroup = e.MuscleGroup,
                    DifficultyLevel = e.DifficultyLevel,
                    EquipmentIds = e.EquipmentRequirements.Select(r => r.EquipmentId).ToList(),
                    EquipmentNames = string.Join(", ", e.EquipmentRequirements.Select(r => r.Equipment.Name))
                }).ToList();

                var pagedResult = new PagedResult<ExerciseCatalogItemDto>
                {
                    Items = dtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                };

                return Result<PagedResult<ExerciseCatalogItemDto>>.Success(pagedResult);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Result<PagedResult<ExerciseCatalogItemDto>>.Failure(_localizer["AnErrorOccurredRetrievingExerciseCatalogItems"]);
            }
        }

        public async Task<Result<ExerciseCatalogItemDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var entity = await _exerciseCatalogRepository.GetAll()
                    .AsNoTracking()
                    .Include(x => x.EquipmentRequirements)
                        .ThenInclude(r => r.Equipment)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (entity == null)
                    return Result<ExerciseCatalogItemDto>.Failure(_localizer["ExerciseCatalogItemNotFound"]);

                var dto = new ExerciseCatalogItemDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    Description = entity.Description,
                    MuscleGroup = entity.MuscleGroup,
                    DifficultyLevel = entity.DifficultyLevel,
                    EquipmentIds = entity.EquipmentRequirements.Select(r => r.EquipmentId).ToList(),
                    EquipmentNames = string.Join(", ", entity.EquipmentRequirements.Select(r => r.Equipment.Name))
                };

                return Result<ExerciseCatalogItemDto>.Success(dto);
            }
            catch (Exception)
            {
                return Result<ExerciseCatalogItemDto>.Failure(_localizer["AnErrorOccurredRetrievingExerciseCatalogItem"]);
            }
        }

        public async Task<Result<Guid>> CreateAsync(ExerciseCatalogItemDto dto)
        {
            try
            {
                var entity = new ExerciseCatalogItem
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    MuscleGroup = dto.MuscleGroup,
                    DifficultyLevel = dto.DifficultyLevel
                };

                await _exerciseCatalogRepository.AddAsync(entity);

                foreach (var equipmentId in dto.EquipmentIds)
                {
                    var requirement = new ExerciseEquipmentRequirement
                    {
                        ExerciseCatalogItemId = entity.Id,
                        EquipmentId = equipmentId
                    };
                    await _requirementRepository.AddAsync(requirement);
                }

                return Result<Guid>.Success(entity.Id);
            }
            catch (Exception)
            {
                return Result<Guid>.Failure(_localizer["AnErrorOccurredCreatingExerciseCatalogItem"]);
            }
        }

        public async Task<Result<bool>> UpdateAsync(ExerciseCatalogItemDto dto)
        {
            try
            {
                var entity = await _exerciseCatalogRepository.GetAll()
                    .Include(x => x.EquipmentRequirements)
                    .FirstOrDefaultAsync(x => x.Id == dto.Id);

                if (entity == null)
                    return Result<bool>.Failure(_localizer["ExerciseCatalogItemNotFound"]);

                entity.Name = dto.Name;
                entity.Description = dto.Description;
                entity.MuscleGroup = dto.MuscleGroup;
                entity.DifficultyLevel = dto.DifficultyLevel;

                await _exerciseCatalogRepository.UpdateAsync(entity);

                // Remove old equipment requirements
                foreach (var oldRequirement in entity.EquipmentRequirements.ToList())
                {
                    await _requirementRepository.HardDeleteAsync(oldRequirement);
                }

                // Add new equipment requirements
                foreach (var equipmentId in dto.EquipmentIds)
                {
                    var requirement = new ExerciseEquipmentRequirement
                    {
                        ExerciseCatalogItemId = entity.Id,
                        EquipmentId = equipmentId
                    };
                    await _requirementRepository.AddAsync(requirement);
                }

                return Result<bool>.Success(true);
            }
            catch (Exception)
            {
                return Result<bool>.Failure(_localizer["AnErrorOccurredUpdatingExerciseCatalogItem"]);
            }
        }

        public async Task<Result<bool>> DeleteAsync(Guid id)
        {
            try
            {
                var entity = await _exerciseCatalogRepository.GetAll()
                    .Include(x => x.EquipmentRequirements)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (entity == null)
                    return Result<bool>.Failure(_localizer["ExerciseCatalogItemNotFound"]);

                // Delete related equipment requirements first
                foreach (var requirement in entity.EquipmentRequirements.ToList())
                {
                    await _requirementRepository.HardDeleteAsync(requirement);
                }

                await _exerciseCatalogRepository.HardDeleteAsync(entity);
                return Result<bool>.Success(true);
            }
            catch (Exception)
            {
                return Result<bool>.Failure(_localizer["AnErrorOccurredDeletingExerciseCatalogItem"]);
            }
        }
    }
}
