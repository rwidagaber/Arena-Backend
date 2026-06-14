using ArenaApplication.Dtos.ProgressLogDtos;
using ArenaDomain.Entities.Health;
using ArenaDomain.Interfaces;
using ArenaDomain.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using ArenaApplication.IServices.IProgressServices;

namespace ArenaApplication.Services
{
    public class ProgressService : IProgressService
    {
        private readonly IProgressRepository _progressRepository;

        public ProgressService(IProgressRepository progressRepository)
        {
            _progressRepository = progressRepository;
        }

        public async Task<Result<ProgressSummaryDto>> GetProgressAsync(Guid memberProfileId)
        {
            var logs = await _progressRepository.GetByMemberProfileIdAsync(memberProfileId);

            if (!logs.Any())
                return Result<ProgressSummaryDto>.Success(new ProgressSummaryDto());

            var latest = logs.Last();
            var previous = logs.Count > 1 ? logs[^2] : null;

            var summary = new ProgressSummaryDto
            {
                CurrentWeight = latest.Weight,
                CurrentBodyFat = latest.BodyFat,
                CurrentMuscleMass = latest.MuscleMass,
                WeightChange = previous != null ? latest.Weight - previous.Weight : null,
                BodyFatChange = previous != null ? latest.BodyFat - previous.BodyFat : null,
                MuscleMassChange = previous != null ? latest.MuscleMass - previous.MuscleMass : null,
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
                LoggedAt = dto.LoggedAt
            };

            await _progressRepository.AddAsync(log);

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

