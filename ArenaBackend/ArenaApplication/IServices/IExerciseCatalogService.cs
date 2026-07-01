using ArenaApplication.Dtos.Workout;
using ArenaApplication.Dtos.UserSubscription;
using ArenaDomain.Shared;
using System;
using System.Threading.Tasks;

namespace ArenaApplication.IServices
{
    public interface IExerciseCatalogService
    {
        Task<Result<PagedResult<ExerciseCatalogItemDto>>> GetAllAsync(string? search, int page, int pageSize);
        Task<Result<ExerciseCatalogItemDto>> GetByIdAsync(Guid id);
        Task<Result<Guid>> CreateAsync(ExerciseCatalogItemDto dto);
        Task<Result<bool>> UpdateAsync(ExerciseCatalogItemDto dto);
        Task<Result<bool>> DeleteAsync(Guid id);
    }
}
