using ArenaDomain.Entities;
using ArenaDomain.Entities.User;
using ArenaDomain.Interfaces;
using ArenaDomain.Shared;
using ArenaInfrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaInfrastructure.Repositories
{
    public class AuthRepository : IAuthRepository
    {
         private readonly AppDbContext _context;

        public AuthRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ApplicationUser?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.MemberProfile)
                    .ThenInclude(m => m!.Subscriptions)
                        .ThenInclude(s => s.Plan)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<ApplicationUser?> GetByIdWithProfileAsync(Guid userId)
        {
            return await _context.Users
                .Include(u => u.MemberProfile)
                    .ThenInclude(m => m!.Subscriptions)
                        .ThenInclude(s => s.Plan)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task SaveRefreshTokenAsync(RefreshToken token)
        {
            await _context.RefreshTokens.AddAsync(token);
            await _context.SaveChangesAsync();
        }

        public async Task<RefreshToken?> GetRefreshTokenAsync(string token, Guid userId)
        {
            return await _context.RefreshTokens
                .FirstOrDefaultAsync(t =>
                    t.Token      == token   &&
                    t.UserId     == userId  &&
                    !t.IsRevoked            &&
                    t.ExpiresAt  > DateTime.UtcNow);
        }

        public async Task RevokeRefreshTokenAsync(RefreshToken token)
        {
            token.IsRevoked = true;
            _context.RefreshTokens.Update(token);
            await _context.SaveChangesAsync();
        }

        public async Task RevokeAllRefreshTokensAsync(Guid userId)
        {
            var tokens = await _context.RefreshTokens
                .Where(t => t.UserId == userId && !t.IsRevoked)
                .ToListAsync();

            foreach (var token in tokens)
                token.IsRevoked = true;

            await _context.SaveChangesAsync();
        }

        public async Task CreateMemberProfileAsync(MemberProfile memberProfile)
        {
            await _context.MemberProfiles.AddAsync(memberProfile);
            await _context.SaveChangesAsync();
        }


        public async Task UpdateMemberProfileAsync(MemberProfile memberProfile)
        {
            _context.MemberProfiles.Update(memberProfile);
            await _context.SaveChangesAsync();
        }


    }
}

