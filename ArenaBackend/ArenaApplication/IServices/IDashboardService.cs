using ArenaApplication.Dtos.Dashboard;
using ArenaApplication.Dtos.Dashboard.Analytics;

namespace ArenaApplication.IServices
{
    public interface IDashboardService
    {
        Task<AdminDashboardDto> GetDashboardDataAsync(CancellationToken cancellationToken = default);
        Task<AnalyticsEnvelopeDto<AdminAnalyticsV2Dto>> GetAnalyticsV2Async(
            AnalyticsQueryWindowDto query,
            CancellationToken cancellationToken = default);
        Task<AnalyticsEnvelopeDto<List<DailyMetricPointDto>>> GetRevenueDrilldownAsync(
            AnalyticsQueryWindowDto query,
            CancellationToken cancellationToken = default);
        Task<AnalyticsEnvelopeDto<List<DailyMetricPointDto>>> GetAttendanceDrilldownAsync(
            AnalyticsQueryWindowDto query,
            CancellationToken cancellationToken = default);
    }
}
