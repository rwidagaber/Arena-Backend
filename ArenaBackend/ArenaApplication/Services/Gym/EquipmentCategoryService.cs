using ArenaApplication.Dtos.Gym;
using ArenaApplication.IServices;
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
    public class EquipmentCategoryService : IEquipmentCategoryService
    {
        private readonly IGenericRepository<EquipmentCategory, Guid> _categoryRepository;
        private readonly IGenericRepository<Equipment, Guid> _equipmentRepository;
        private readonly IStringLocalizer<ArenaLocalization> _localizer;

        public EquipmentCategoryService(
            IGenericRepository<EquipmentCategory, Guid> categoryRepository,
            IGenericRepository<Equipment, Guid> equipmentRepository,
            IStringLocalizer<ArenaLocalization> localizer)
        {
            _categoryRepository = categoryRepository;
            _equipmentRepository = equipmentRepository;
            _localizer = localizer;
        }

        public async Task<Result<List<EquipmentCategoryDto>>> GetAllCategoriesAsync()
        {
            try
            {
                var categories = await _categoryRepository.GetAll()
                    .AsNoTracking()
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                var dtos = categories.Select(c => new EquipmentCategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    NameAr = c.NameAr
                }).ToList();

                return Result<List<EquipmentCategoryDto>>.Success(dtos);
            }
            catch (Exception)
            {
                return Result<List<EquipmentCategoryDto>>.Failure(_localizer["AnErrorOccurredRetrievingCategories"]);
            }
        }

        public async Task<Result<EquipmentCategoryDto>> GetCategoryByIdAsync(Guid id)
        {
            try
            {
                var category = await _categoryRepository.GetByIdAsync(id);
                if (category == null)
                    return Result<EquipmentCategoryDto>.Failure(_localizer["CategoryNotFound"]);

                var dto = new EquipmentCategoryDto
                {
                    Id = category.Id,
                    Name = category.Name,
                    NameAr = category.NameAr
                };

                return Result<EquipmentCategoryDto>.Success(dto);
            }
            catch (Exception)
            {
                return Result<EquipmentCategoryDto>.Failure(_localizer["AnErrorOccurredRetrievingCategory"]);
            }
        }

        public async Task<Result<Guid>> CreateCategoryAsync(EquipmentCategoryDto dto)
        {
            try
            {
                var category = new EquipmentCategory
                {
                    Name = dto.Name,
                    NameAr = dto.NameAr
                };

                await _categoryRepository.AddAsync(category);
                return Result<Guid>.Success(category.Id);
            }
            catch (Exception)
            {
                return Result<Guid>.Failure(_localizer["AnErrorOccurredCreatingCategory"]);
            }
        }

        public async Task<Result<bool>> UpdateCategoryAsync(EquipmentCategoryDto dto)
        {
            try
            {
                var category = await _categoryRepository.GetByIdAsync(dto.Id);
                if (category == null)
                    return Result<bool>.Failure(_localizer["CategoryNotFound"]);

                string oldName = category.Name;
                string newName = dto.Name;

                category.Name = dto.Name;
                category.NameAr = dto.NameAr;

                await _categoryRepository.UpdateAsync(category);

                // Cascade changes to Equipment records
                if (!string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase))
                {
                    var equipments = await _equipmentRepository.GetAll().ToListAsync();
                    foreach (var eq in equipments)
                    {
                        if (string.IsNullOrWhiteSpace(eq.Category)) continue;

                        var parts = eq.Category.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                               .Select(p => p.Trim())
                                               .ToList();

                        bool modified = false;
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
                            eq.Category = string.Join(", ", parts);
                            await _equipmentRepository.UpdateAsync(eq);
                        }
                    }
                }

                return Result<bool>.Success(true);
            }
            catch (Exception)
            {
                return Result<bool>.Failure(_localizer["AnErrorOccurredUpdatingCategory"]);
            }
        }

        public async Task<Result<bool>> DeleteCategoryAsync(Guid id)
        {
            try
            {
                var category = await _categoryRepository.GetByIdAsync(id);
                if (category == null)
                    return Result<bool>.Failure(_localizer["CategoryNotFound"]);

                string nameToRemove = category.Name;

                await _categoryRepository.HardDeleteAsync(category);

                // Cascade changes (removal) to Equipment records
                var equipments = await _equipmentRepository.GetAll().ToListAsync();
                foreach (var eq in equipments)
                {
                    if (string.IsNullOrWhiteSpace(eq.Category)) continue;

                    var parts = eq.Category.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                           .Select(p => p.Trim())
                                           .Where(p => !string.Equals(p, nameToRemove, StringComparison.OrdinalIgnoreCase))
                                           .ToList();

                    string newCategoryString = string.Join(", ", parts);
                    if (eq.Category != newCategoryString)
                    {
                        eq.Category = newCategoryString;
                        await _equipmentRepository.UpdateAsync(eq);
                    }
                }

                return Result<bool>.Success(true);
            }
            catch (Exception)
            {
                return Result<bool>.Failure(_localizer["AnErrorOccurredDeletingCategory"]);
            }
        }
    }
}
