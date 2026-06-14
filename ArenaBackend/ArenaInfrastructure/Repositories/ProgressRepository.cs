using ArenaDomain.Entities.Health;
using ArenaDomain.Interfaces;
using ArenaInfrastructure.Data;
using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Text;

namespace ArenaInfrastructure.Repositories
{
    public class ProgressRepository : IProgressRepository
    {
        private readonly AppDbContext _context;

        public ProgressRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<ProgressLog>> GetByMemberProfileIdAsync(Guid memberProfileId)
        {
            return await _context.ProgressLogs
                .Where(p => p.MemberProfileId == memberProfileId)
                .OrderBy(p => p.LoggedAt)
                .ToListAsync();
        }

        public async Task<ProgressLog?> GetLatestAsync(Guid memberProfileId)
        {
            return await _context.ProgressLogs
                .Where(p => p.MemberProfileId == memberProfileId)
                .OrderByDescending(p => p.LoggedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<ProgressLog?> GetByIdAsync(Guid id)
        {
            return await _context.ProgressLogs
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task AddAsync(ProgressLog log)
        {
            await _context.ProgressLogs.AddAsync(log);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(ProgressLog log)
        {
            _context.ProgressLogs.Remove(log);
            await _context.SaveChangesAsync();
        }
    }
}
