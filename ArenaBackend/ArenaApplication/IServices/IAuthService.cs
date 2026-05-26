using ArenaApplication.Dtos.AuthDtos;
using System;
using System.Collections.Generic;
using System.Text;
using ArenaApplication.Dtos;
using ArenaApplication.Dtos.RegisterDto;
using ArenaApplication.Dtos.loginDto;
using ArenaDomain.Entities;

namespace ArenaApplication.IServices
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(UserRegisterDto dto);
        Task<AuthResponseDto> LoginAsync(UserloginDto dto);
        Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto dto);
        Task LogoutAsync(string userId);
        Task<MemberProfile> GetProfileAsync(string userId);
        Task ChangePasswordAsync(string userId, ChangePasswordDto dto);
        Task ForgotPasswordAsync(ForgotPasswordDto dto);
        Task ResetPasswordAsync(ResetPasswordDto dto);
    }
}
