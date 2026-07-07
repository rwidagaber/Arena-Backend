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
            var profile = await _context.MemberProfiles
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (profile != null && profile.User == null)
            {
                await _context.Entry(profile).Reference(x => x.User).LoadAsync(cancellationToken);
            }

            return profile;
        }

        public async Task UpdateAsync(MemberProfile memberProfile)
        {
            _context.MemberProfiles.Update(memberProfile);
            await _context.SaveChangesAsync();
        }
    }
}
