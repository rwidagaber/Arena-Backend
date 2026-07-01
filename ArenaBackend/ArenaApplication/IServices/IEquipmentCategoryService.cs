using ArenaApplication.Dtos.Gym;
using ArenaDomain.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArenaApplication.IServices
{
    public interface IEquipmentCategoryService
    {
        Task<Result<List<EquipmentCategoryDto>>> GetAllCategoriesAsync();
        Task<Result<EquipmentCategoryDto>> GetCategoryByIdAsync(Guid id);
        Task<Result<Guid>> CreateCategoryAsync(EquipmentCategoryDto dto);
        Task<Result<bool>> UpdateCategoryAsync(EquipmentCategoryDto dto);
        Task<Result<bool>> DeleteCategoryAsync(Guid id);
    }
}
