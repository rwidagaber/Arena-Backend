using ArenaApplication.Dtos.ProgressLogDtos;
using ArenaDomain.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.IServices.IProgressServices
{
    public interface IProgressService
    {
        Task<Result<ProgressSummaryDto>> GetProgressAsync(Guid memberProfileId);
        Task<Result<ProgressLogDto>> LogProgressAsync(Guid memberProfileId, CreateProgressLogDto dto);
        Task<Result> DeleteLogAsync(Guid memberProfileId, Guid logId);
    }
}
