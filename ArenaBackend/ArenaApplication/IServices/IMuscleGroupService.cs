using ArenaApplication.Dtos.Workout;
using ArenaDomain.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArenaApplication.IServices
{
    public interface IMuscleGroupService
    {
        Task<Result<List<MuscleGroupDto>>> GetAllMuscleGroupsAsync();
        Task<Result<MuscleGroupDto>> GetMuscleGroupByIdAsync(Guid id);
        Task<Result<Guid>> CreateMuscleGroupAsync(MuscleGroupDto dto);
        Task<Result<bool>> UpdateMuscleGroupAsync(MuscleGroupDto dto);
        Task<Result<bool>> DeleteMuscleGroupAsync(Guid id);
    }
}
