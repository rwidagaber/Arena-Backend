using ArenaDomain.Entities.Gym;
using System.Threading;
using System.Threading.Tasks;

namespace ArenaApplication.IServices
{
    public interface IGymSettingsService
    {
        Task<int> GetNoShowThresholdAsync(CancellationToken cancellationToken = default);
        Task UpdateNoShowThresholdAsync(int newThreshold, CancellationToken cancellationToken = default);
        Task<GymSetting> GetGymSettingsAsync(CancellationToken cancellationToken = default);
        Task UpdateGymSettingsAsync(int threshold, bool isEnabled, CancellationToken cancellationToken = default);
    }
}
