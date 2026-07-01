using ArenaApplication.Dtos.Gym;
using ArenaDomain.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using ArenaApplication.Dtos.UserSubscription;

namespace ArenaApplication.IServices
{
    public interface IEquipmentService
    {
        Task<Result<PagedResult<EquipmentDto>>> GetAllEquipmentsAsync(string? search, int page, int pageSize);
        Task<Result<EquipmentDto>> GetEquipmentByIdAsync(Guid id);
        Task<Result<Guid>> CreateEquipmentAsync(EquipmentDto equipmentDto);
        Task<Result<bool>> UpdateEquipmentAsync(EquipmentDto equipmentDto);
        Task<Result<bool>> DeleteEquipmentAsync(Guid id);
        Task<Result<List<string>>> GetCategoriesAsync();
    }
}
