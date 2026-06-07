using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using ArenaApplication.IServices.User;
using ArenaInfrastructure.Data;
using ArenaDomain.Entities.User;
using Microsoft.EntityFrameworkCore;

namespace ArenaInfrastructure.Services
{
    public class UserQueryService : IUserQueryService
    {
        private readonly AppDbContext _context;

        public UserQueryService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ApplicationUser?> GetByIdAsync(Guid userId)
    => await _context.Users
        .FirstOrDefaultAsync(u => u.Id == userId);
    }
}
