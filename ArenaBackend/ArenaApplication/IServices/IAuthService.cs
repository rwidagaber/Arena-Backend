using ArenaApplication.Dtos;
using ArenaApplication.Dtos.AuthDtos;
using ArenaApplication.Dtos.loginDto;
using ArenaApplication.Dtos.ProfileDtos;
using ArenaApplication.Dtos.RegisterDto;
using ArenaDomain.Entities;
using ArenaDomain.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.IServices
{
    public interface IAuthService
    {
        Task<Result> RegisterAsync(UserRegisterDto dto);
        Task<Result<AuthResponseDto>> LoginAsync(UserloginDto dto);
        Task<Result<AuthResponseDto>> RefreshTokenAsync(RefreshTokenDto dto);
        Task<Result> LogoutAsync(Guid userId);
        Task<Result<GetProfileDto>> GetProfileAsync(Guid userId);
        Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
        Task<Result> ForgotPasswordAsync(ForgotPasswordDto dto);
        Task<Result> ResetPasswordAsync(ResetPasswordDto dto);
        Task<Result<AuthResponseDto>> ConfirmEmailAsync(ConfirmEmailDto dto);

    }
}
