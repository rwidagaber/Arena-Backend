using ArenaApplication.IServices;
using ArenaDomain.Entities.Gym;
using ArenaDomain.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ArenaApplication.Services.Gym
{
    public class GymSettingsService : IGymSettingsService
    {
        private readonly IGenericRepository<GymSetting, int> _repository;
        private readonly IUnitOfWork _unitOfWork;

        public GymSettingsService(
            IGenericRepository<GymSetting, int> repository,
            IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<int> GetNoShowThresholdAsync(CancellationToken cancellationToken = default)
        {
            var settings = await _repository.GetAllAsync(cancellationToken);
            var setting = settings.FirstOrDefault();
            if (setting == null)
            {
                return 2; // Default fallback
            }
            return setting.NoShowThreshold;
        }

        public async Task UpdateNoShowThresholdAsync(int newThreshold, CancellationToken cancellationToken = default)
        {
            if (newThreshold < 1)
            {
                throw new ArgumentException("Threshold must be at least 1.");
            }

            var settings = await _repository.GetAllAsync(cancellationToken);
            var setting = settings.FirstOrDefault();

            if (setting == null)
            {
                setting = new GymSetting
                {
                    Id = 1,
                    NoShowThreshold = newThreshold,
                    CreatedAt = DateTime.UtcNow
                };
                await _repository.AddAsync(setting, cancellationToken);
            }
            else
            {
                setting.NoShowThreshold = newThreshold;
                setting.UpdatedAt = DateTime.UtcNow;
                await _repository.UpdateAsync(setting, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task<GymSetting> GetGymSettingsAsync(CancellationToken cancellationToken = default)
        {
            var settings = await _repository.GetAllAsync(cancellationToken);
            var setting = settings.FirstOrDefault();
            if (setting == null)
            {
                // Return seeded default fallback
                return new GymSetting
                {
                    Id = 1,
                    NoShowThreshold = 2,
                    IsNoShowPenaltyEnabled = true,
                    CreatedAt = DateTime.UtcNow
                };
            }
            return setting;
        }

        public async Task UpdateGymSettingsAsync(int threshold, bool isEnabled, CancellationToken cancellationToken = default)
        {
            if (threshold < 1)
            {
                throw new ArgumentException("Threshold must be at least 1.");
            }

            var settings = await _repository.GetAllAsync(cancellationToken);
            var setting = settings.FirstOrDefault();

            if (setting == null)
            {
                setting = new GymSetting
                {
                    Id = 1,
                    NoShowThreshold = threshold,
                    IsNoShowPenaltyEnabled = isEnabled,
                    CreatedAt = DateTime.UtcNow
                };
                await _repository.AddAsync(setting, cancellationToken);
            }
            else
            {
                setting.NoShowThreshold = threshold;
                setting.IsNoShowPenaltyEnabled = isEnabled;
                setting.UpdatedAt = DateTime.UtcNow;
                await _repository.UpdateAsync(setting, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
