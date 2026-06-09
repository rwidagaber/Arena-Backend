using ArenaDomain.Entities.User;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace ArenaApplication.IServices
{
    public interface ITokenService
    {
        Task<string> GenerateAccessToken(ApplicationUser user);
        string GenerateRefreshToken();
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);

    }
}
