using ArenaApplication.Dtos.Gym;
using ArenaApplication.Dtos.UserSubscription;
using ArenaApplication.IServices;
using ArenaDomain.Entities.Gym;
using ArenaDomain.Interfaces;
using ArenaDomain.Shared;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ArenaApplication.Services.Gym
{
    public class EquipmentService : IEquipmentService
    {
        private readonly IGenericRepository<Equipment, Guid> _equipmentRepository;
        private readonly IGenericRepository<EquipmentCategory, Guid> _categoryRepository;
        private readonly IStringLocalizer<ArenaLocalization> _localizer;

        public EquipmentService(
            IGenericRepository<Equipment, Guid> equipmentRepository,
            IGenericRepository<EquipmentCategory, Guid> categoryRepository,
            IStringLocalizer<ArenaLocalization> localizer)
        {
            _equipmentRepository = equipmentRepository;
            _categoryRepository = categoryRepository;
            _localizer = localizer;
        }

        public async Task<Result<PagedResult<EquipmentDto>>> GetAllEquipmentsAsync(string? search, int page, int pageSize)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 10;

                var query = _equipmentRepository.GetAll().AsNoTracking();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var cleanSearch = search.Trim().ToLower();
                    
                    var matchingCategories = await _categoryRepository.GetAll()
                        .AsNoTracking()
                        .Where(c => c.Name.ToLower().Contains(cleanSearch) || c.NameAr.ToLower().Contains(cleanSearch))
                        .Select(c => c.Name.ToLower())
                        .ToListAsync();

                    query = query.Where(e => 
                        e.Name.ToLower().Contains(cleanSearch) || 
                        (e.NameAr != null && e.NameAr.ToLower().Contains(cleanSearch)) ||
                        e.Category.ToLower().Contains(cleanSearch) ||
                        matchingCategories.Any(mc => e.Category.ToLower().Contains(mc)));
                }

                int totalCount = await query.CountAsync();

                var equipments = await query
                    .OrderBy(e => e.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var categoriesList = await _categoryRepository.GetAll().AsNoTracking().ToListAsync();
                var currentCulture = System.Globalization.CultureInfo.CurrentUICulture.Name;
                var isArabic = currentCulture.StartsWith("ar", StringComparison.OrdinalIgnoreCase);

                var categoryMap = categoriesList.ToDictionary(
                    c => c.Name,
                    c => isArabic && !string.IsNullOrEmpty(c.NameAr) ? c.NameAr : c.Name,
                    StringComparer.OrdinalIgnoreCase
                );

                var dtos = equipments.Select(e =>
                {
                    var localizedCategory = string.Empty;
                    if (!string.IsNullOrWhiteSpace(e.Category))
                    {
                        var parts = e.Category.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                              .Select(p => p.Trim());
                        var localizedParts = parts.Select(p => categoryMap.TryGetValue(p, out var loc) ? loc : p);
                        localizedCategory = string.Join(", ", localizedParts);
                    }

                    return new EquipmentDto
                    {
                        Id = e.Id,
                        Name = e.Name,
                        NameAr = e.NameAr,
                        Category = localizedCategory,
                        IsAvailable = e.IsAvailable
                    };
                }).ToList();

                var pagedResult = new PagedResult<EquipmentDto>
                {
                    Items = dtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                };

                return Result<PagedResult<EquipmentDto>>.Success(pagedResult);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Result<PagedResult<EquipmentDto>>.Failure(_localizer["AnErrorOccurredRetrievingEquipments"]);
            }
        }

        public async Task<Result<EquipmentDto>> GetEquipmentByIdAsync(Guid id)
        {
            try
            {
                var equipment = await _equipmentRepository.GetByIdAsync(id);
                if (equipment == null)
                    return Result<EquipmentDto>.Failure(_localizer["EquipmentNotFound"]);

                var dto = new EquipmentDto
                {
                    Id = equipment.Id,
                    Name = equipment.Name,
                    NameAr = equipment.NameAr,
                    Category = equipment.Category,
                    IsAvailable = equipment.IsAvailable
                };

                return Result<EquipmentDto>.Success(dto);
            }
            catch (Exception)
            {
                return Result<EquipmentDto>.Failure(_localizer["AnErrorOccurredRetrievingEquipment"]);
            }
        }

        public async Task<Result<Guid>> CreateEquipmentAsync(EquipmentDto equipmentDto)
        {
            try
            {
                var equipment = new Equipment
                {
                    Name = equipmentDto.Name,
                    NameAr = equipmentDto.NameAr,
                    Category = equipmentDto.Category,
                    IsAvailable = equipmentDto.IsAvailable
                };

                await _equipmentRepository.AddAsync(equipment);
                return Result<Guid>.Success(equipment.Id);
            }
            catch (Exception)
            {
                return Result<Guid>.Failure(_localizer["AnErrorOccurredCreatingEquipment"]);
            }
        }

        public async Task<Result<bool>> UpdateEquipmentAsync(EquipmentDto equipmentDto)
        {
            try
            {
                var equipment = await _equipmentRepository.GetByIdAsync(equipmentDto.Id);
                if (equipment == null)
                    return Result<bool>.Failure(_localizer["EquipmentNotFound"]);

                equipment.Name = equipmentDto.Name;
                equipment.NameAr = equipmentDto.NameAr;
                equipment.Category = equipmentDto.Category;
                equipment.IsAvailable = equipmentDto.IsAvailable;

                await _equipmentRepository.UpdateAsync(equipment);
                return Result<bool>.Success(true);
            }
            catch (Exception)
            {
                return Result<bool>.Failure(_localizer["AnErrorOccurredUpdatingEquipment"]);
            }
        }

        public async Task<Result<bool>> DeleteEquipmentAsync(Guid id)
        {
            try
            {
                var equipment = await _equipmentRepository.GetByIdAsync(id);
                if (equipment == null)
                    return Result<bool>.Failure(_localizer["EquipmentNotFound"]);

                await _equipmentRepository.HardDeleteAsync(equipment);
                return Result<bool>.Success(true);
            }
            catch (Exception)
            {
                return Result<bool>.Failure(_localizer["AnErrorOccurredDeletingEquipment"]);
            }
        }

        public async Task<Result<List<string>>> GetCategoriesAsync()
        {
            try
            {
                var categories = await _categoryRepository.GetAll()
                    .AsNoTracking()
                    .OrderBy(c => c.Name)
                    .Select(c => c.Name)
                    .ToListAsync();

                return Result<List<string>>.Success(categories);
            }
            catch (Exception)
            {
                return Result<List<string>>.Failure(_localizer["AnErrorOccurredRetrievingCategories"]);
            }
        }
    }
}
