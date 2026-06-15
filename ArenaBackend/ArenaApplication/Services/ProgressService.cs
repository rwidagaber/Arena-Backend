using ArenaApplication.Dtos.ProgressLogDtos;
using ArenaApplication.IServices.IProgressServices;
using ArenaDomain.Entities.Health;
using ArenaDomain.Interfaces;
using ArenaDomain.Shared;
using ArenaInfrastructure.Repositories;

namespace ArenaApplication.Services
{
    public class ProgressService : IProgressService
    {
        private readonly IProgressRepository _progressRepository;
        private readonly IMemberProfileRepository _memberProfileRepository;

        public ProgressService(
            IProgressRepository progressRepository,
            IMemberProfileRepository memberProfileRepository)
        {
            _progressRepository = progressRepository;
            _memberProfileRepository = memberProfileRepository;
        }

        public async Task<Result<ProgressSummaryDto>> GetProgressAsync(Guid memberProfileId)
        {
            var logs = await _progressRepository
                .GetByMemberProfileIdAsync(memberProfileId);

            var memberProfile = await _memberProfileRepository
                .GetByIdAsync(memberProfileId);

            if (memberProfile is null)
                return Result<ProgressSummaryDto>.Failure("Member profile not found");

            if (!logs.Any())
                return Result<ProgressSummaryDto>.Success(new ProgressSummaryDto
                {
                    CurrentWeight = memberProfile.Weight ?? 0,
                    CurrentBodyFat = null,
                    CurrentMuscleMass = null,
                    WeightChange = null,
                    BodyFatChange = null,
                    MuscleMassChange = null,
                    Logs = []
                });

            var latest = logs.Last();
            var previous = logs.Count > 1 ? logs[^2] : null;

            // Fix CS0173 — explicit decimal? cast
            var weightChange = previous != null
                ? (decimal?)(latest.Weight - previous.Weight)
                : memberProfile.Weight.HasValue
                    ? (decimal?)(latest.Weight - memberProfile.Weight.Value)
                    : null;

            var bodyFatChange = previous != null
                ? (decimal?)(latest.BodyFat - previous.BodyFat)
                : null;

            var muscleMassChange = previous != null
                ? (decimal?)(latest.MuscleMass - previous.MuscleMass)
                : memberProfile.MuscleMass.HasValue && latest.MuscleMass.HasValue
                    ? (decimal?)(latest.MuscleMass.Value - memberProfile.MuscleMass.Value)
                    : null;

            var summary = new ProgressSummaryDto
            {
                CurrentWeight = latest.Weight,
                CurrentBodyFat = latest.BodyFat,
                CurrentMuscleMass = latest.MuscleMass,
                WeightChange = weightChange,
                BodyFatChange = bodyFatChange,
                MuscleMassChange = muscleMassChange,
                Logs = logs.Select(l => new ProgressLogDto
                {
                    Id = l.Id,
                    Weight = l.Weight,
                    BodyFat = l.BodyFat,
                    MuscleMass = l.MuscleMass,
                    LoggedAt = l.LoggedAt
                }).ToList()
            };

            return Result<ProgressSummaryDto>.Success(summary);
        }

        public async Task<Result<ProgressLogDto>> LogProgressAsync(
            Guid memberProfileId, CreateProgressLogDto dto)
        {
            var log = new ProgressLog
            {
                MemberProfileId = memberProfileId,
                Weight = dto.Weight,
                BodyFat = dto.BodyFat,
                MuscleMass = dto.MuscleMass,
                LoggedAt = DateTime.UtcNow
            };

            await _progressRepository.AddAsync(log);

            var memberProfile = await _memberProfileRepository
                .GetByIdAsync(memberProfileId);

            if (memberProfile is not null)
            {
                memberProfile.Weight = dto.Weight;
                memberProfile.MuscleMass = dto.MuscleMass ?? memberProfile.MuscleMass; // Fix CS1503

                // Recalculate BMI
                if (memberProfile.Height.HasValue && memberProfile.Height > 0)
                {
                    var heightInMeters = memberProfile.Height.Value / 100;
                    memberProfile.BMI = Math.Round(
                        dto.Weight / (heightInMeters * heightInMeters), 2);
                }

                await _memberProfileRepository.UpdateAsync(memberProfile);
            }

            return Result<ProgressLogDto>.Success(new ProgressLogDto
            {
                Id = log.Id,
                Weight = log.Weight,
                BodyFat = log.BodyFat,
                MuscleMass = log.MuscleMass,
                LoggedAt = log.LoggedAt
            });
        }

        public async Task<Result> DeleteLogAsync(Guid memberProfileId, Guid logId)
        {
            var log = await _progressRepository.GetByIdAsync(logId);

            if (log is null)
                return Result.Failure("Log not found");

            if (log.MemberProfileId != memberProfileId)
                return Result.Failure("Unauthorized");

            await _progressRepository.DeleteAsync(log);
            return Result.Success();
        }
    }
}