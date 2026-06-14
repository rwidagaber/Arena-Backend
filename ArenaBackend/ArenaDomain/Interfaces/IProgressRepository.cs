using ArenaDomain.Entities.Health;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaDomain.Interfaces
{
    public interface IProgressRepository
    {
        Task<List<ProgressLog>> GetByMemberProfileIdAsync(Guid memberProfileId);
        Task<ProgressLog?> GetLatestAsync(Guid memberProfileId);
        Task<ProgressLog?> GetByIdAsync(Guid id);
        Task AddAsync(ProgressLog log);
        Task DeleteAsync(ProgressLog log);
    }
}
