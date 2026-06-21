using ArenaApplication.Dtos.Gym;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ArenaApplication.Services.Gym
{
    public interface IWorkingHoursService
    {
        Task<IEnumerable<WorkingHoursDto>> GetWorkingHoursAsync(CancellationToken cancellationToken = default);
        Task<WorkingHoursDto> UpdateWorkingHoursAsync(int id, UpdateWorkingHoursDto dto, CancellationToken cancellationToken = default);
        Task BulkUpdateWorkingHoursAsync(IEnumerable<int> ids, UpdateWorkingHoursDto dto, CancellationToken cancellationToken = default);
    }
}
