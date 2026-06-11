using ArenaApplication.Dtos.Dashboard;
using ArenaApplication.Dtos.Dashboard.Analytics;

namespace ArenaApplication.IServices
{
    public interface IDashboardService
    {
        Task<AdminDashboardDto> GetDashboardDataAsync(CancellationToken cancellationToken = default);

    }
}
