using ArenaApplication.Dtos.Workout;
using ArenaApplication.IServices;
using ArenaDomain.Entities.Workout;
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
    public class MuscleGroupService : IMuscleGroupService
    {
        private readonly IGenericRepository<MuscleGroup, Guid> _muscleGroupRepository;
        private readonly IGenericRepository<ExerciseCatalogItem, Guid> _exerciseCatalogRepository;
        private readonly IStringLocalizer<ArenaLocalization> _localizer;

        public MuscleGroupService(
            IGenericRepository<MuscleGroup, Guid> muscleGroupRepository,
            IGenericRepository<ExerciseCatalogItem, Guid> exerciseCatalogRepository,
            IStringLocalizer<ArenaLocalization> localizer)
        {
            _muscleGroupRepository = muscleGroupRepository;
            _exerciseCatalogRepository = exerciseCatalogRepository;
            _localizer = localizer;
        }

        public async Task<Result<List<MuscleGroupDto>>> GetAllMuscleGroupsAsync()
        {
            try
            {
                var muscleGroups = await _muscleGroupRepository.GetAll()
                    .AsNoTracking()
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                var dtos = muscleGroups.Select(c => new MuscleGroupDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    NameAr = c.NameAr
                }).ToList();

                return Result<List<MuscleGroupDto>>.Success(dtos);
            }
            catch (Exception)
            {
                return Result<List<MuscleGroupDto>>.Failure(_localizer["AnErrorOccurredRetrievingMuscleGroups"]);
            }
        }

        public async Task<Result<MuscleGroupDto>> GetMuscleGroupByIdAsync(Guid id)
        {
            try
            {
                var muscleGroup = await _muscleGroupRepository.GetByIdAsync(id);
                if (muscleGroup == null)
                    return Result<MuscleGroupDto>.Failure(_localizer["MuscleGroupNotFound"]);

                var dto = new MuscleGroupDto
                {
                    Id = muscleGroup.Id,
                    Name = muscleGroup.Name,
                    NameAr = muscleGroup.NameAr
                };

                return Result<MuscleGroupDto>.Success(dto);
            }
            catch (Exception)
            {
                return Result<MuscleGroupDto>.Failure(_localizer["AnErrorOccurredRetrievingMuscleGroup"]);
            }
        }

        public async Task<Result<Guid>> CreateMuscleGroupAsync(MuscleGroupDto dto)
        {
            try
            {
                var muscleGroup = new MuscleGroup
                {
                    Name = dto.Name,
                    NameAr = dto.NameAr
                };

                await _muscleGroupRepository.AddAsync(muscleGroup);
                return Result<Guid>.Success(muscleGroup.Id);
            }
            catch (Exception)
            {
                return Result<Guid>.Failure(_localizer["AnErrorOccurredCreatingMuscleGroup"]);
            }
        }

        public async Task<Result<bool>> UpdateMuscleGroupAsync(MuscleGroupDto dto)
        {
            try
            {
                var muscleGroup = await _muscleGroupRepository.GetByIdAsync(dto.Id);
                if (muscleGroup == null)
                    return Result<bool>.Failure(_localizer["MuscleGroupNotFound"]);

                string oldName = muscleGroup.Name;
                string newName = dto.Name;
                string oldNameAr = muscleGroup.NameAr;
                string newNameAr = dto.NameAr;

                muscleGroup.Name = dto.Name;
                muscleGroup.NameAr = dto.NameAr;

                await _muscleGroupRepository.UpdateAsync(muscleGroup);

                // Cascade changes to ExerciseCatalogItem records (English name)
                bool englishNameChanged = !string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase);
                bool arabicNameChanged = !string.Equals(oldNameAr, newNameAr, StringComparison.OrdinalIgnoreCase);

                if (englishNameChanged || arabicNameChanged)
                {
                    var exercises = await _exerciseCatalogRepository.GetAll().ToListAsync();
                    foreach (var exercise in exercises)
                    {
                        bool modified = false;

                        if (englishNameChanged && !string.IsNullOrWhiteSpace(exercise.MuscleGroup))
                        {
                            var parts = exercise.MuscleGroup.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                   .Select(p => p.Trim())
                                                   .ToList();

                            for (int i = 0; i < parts.Count; i++)
                            {
                                if (string.Equals(parts[i], oldName, StringComparison.OrdinalIgnoreCase))
                                {
                                    parts[i] = newName;
                                    modified = true;
                                }
                            }

                            if (modified)
                            {
                                exercise.MuscleGroup = string.Join(", ", parts);
                            }
                        }

                        if (arabicNameChanged && !string.IsNullOrWhiteSpace(exercise.MuscleGroupAr))
                        {
                            var partsAr = exercise.MuscleGroupAr.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                   .Select(p => p.Trim())
                                                   .ToList();

                            bool modifiedAr = false;
                            for (int i = 0; i < partsAr.Count; i++)
                            {
                                if (string.Equals(partsAr[i], oldNameAr, StringComparison.OrdinalIgnoreCase))
                                {
                                    partsAr[i] = newNameAr;
                                    modifiedAr = true;
                                }
                            }

                            if (modifiedAr)
                            {
                                exercise.MuscleGroupAr = string.Join(", ", partsAr);
                                modified = true;
                            }
                        }

                        if (modified)
                        {
                            await _exerciseCatalogRepository.UpdateAsync(exercise);
                        }
                    }
                }

                return Result<bool>.Success(true);
            }
            catch (Exception)
            {
                return Result<bool>.Failure(_localizer["AnErrorOccurredUpdatingMuscleGroup"]);
            }
        }

        public async Task<Result<bool>> DeleteMuscleGroupAsync(Guid id)
        {
            try
            {
                var muscleGroup = await _muscleGroupRepository.GetByIdAsync(id);
                if (muscleGroup == null)
                    return Result<bool>.Failure(_localizer["MuscleGroupNotFound"]);

                string nameToRemove = muscleGroup.Name;
                string nameToRemoveAr = muscleGroup.NameAr;

                await _muscleGroupRepository.HardDeleteAsync(muscleGroup);

                // Cascade changes (removal) to ExerciseCatalogItem records
                var exercises = await _exerciseCatalogRepository.GetAll().ToListAsync();
                foreach (var exercise in exercises)
                {
                    bool modified = false;

                    if (!string.IsNullOrWhiteSpace(exercise.MuscleGroup))
                    {
                        var parts = exercise.MuscleGroup.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                               .Select(p => p.Trim())
                                               .Where(p => !string.Equals(p, nameToRemove, StringComparison.OrdinalIgnoreCase))
                                               .ToList();

                        string newMuscleGroupString = string.Join(", ", parts);
                        if (exercise.MuscleGroup != newMuscleGroupString)
                        {
                            exercise.MuscleGroup = newMuscleGroupString;
                            modified = true;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(exercise.MuscleGroupAr))
                    {
                        var partsAr = exercise.MuscleGroupAr.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                               .Select(p => p.Trim())
                                               .Where(p => !string.Equals(p, nameToRemoveAr, StringComparison.OrdinalIgnoreCase))
                                               .ToList();

                        string newMuscleGroupArString = string.Join(", ", partsAr);
                        if (exercise.MuscleGroupAr != newMuscleGroupArString)
                        {
                            exercise.MuscleGroupAr = newMuscleGroupArString;
                            modified = true;
                        }
                    }

                    if (modified)
                      {
                        await _exerciseCatalogRepository.UpdateAsync(exercise);
                    }
                }

                return Result<bool>.Success(true);
            }
            catch (Exception)
            {
                return Result<bool>.Failure(_localizer["AnErrorOccurredDeletingMuscleGroup"]);
            }
        }
    }
}
