using ArenaDomain.Entities;
using ArenaDomain.Entities.User;
using ArenaInfrastructure.Data;
using ArenaInfrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Security.Principal;

namespace ArenaInfrastructure.Repositories
{
    public class MemberProfileRepository : IMemberProfileRepository
    {
        private readonly AppDbContext _context;

        public MemberProfileRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<MemberProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.MemberProfiles
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }
    }
}
