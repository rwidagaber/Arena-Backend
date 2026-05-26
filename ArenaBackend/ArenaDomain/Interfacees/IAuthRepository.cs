using ArenaDomain.Entities;
using ArenaDomain.Entities.User;
using ArenaDomain.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaDomain.Interfacees
{
    public interface IAuthRepository
    {

        Task<ApplicationUser?> GetByEmailAsync(string email);
        Task<ApplicationUser?> GetByIdWithProfileAsync(Guid userId);
        Task SaveRefreshTokenAsync(RefreshToken token);
        Task<RefreshToken?> GetRefreshTokenAsync(string token, Guid userId);
        Task RevokeRefreshTokenAsync(RefreshToken token);
        Task RevokeAllRefreshTokensAsync(Guid userId);
        Task CreateMemberProfileAsync(MemberProfile memberProfile);

    }
}

