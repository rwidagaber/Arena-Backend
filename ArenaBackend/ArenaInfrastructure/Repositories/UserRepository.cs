using ArenaDomain.Entities.User;
using ArenaDomain.Interfaces;
using ArenaInfrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ArenaInfrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public IQueryable<ApplicationUser> GetAll()
        {
            return _context.Users.AsQueryable();
        }

        public async Task<ApplicationUser?> GetByIdAsync(Guid id)
        {
            return await _context.Users
                .Include(u => u.MemberProfile)
                    .ThenInclude(mp => mp.Subscriptions)
                        .ThenInclude(s => s.Plan)
                .Include(u => u.MemberProfile)
                    .ThenInclude(mp => mp.Subscriptions)
                        .ThenInclude(s => s.Payments)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task UpdateAsync(ApplicationUser user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
    }
}
