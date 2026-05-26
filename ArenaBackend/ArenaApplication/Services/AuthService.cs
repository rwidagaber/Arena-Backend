using ArenaApplication.Dtos.AuthDtos;
using ArenaApplication.Dtos.loginDto;
using ArenaApplication.Dtos.ProfileDtos;
using ArenaApplication.Dtos.RegisterDto;
using ArenaApplication.IServices;
using ArenaDomain.Entities;
using ArenaDomain.Entities.User;
using ArenaDomain.Interfacees;
using ArenaDomain.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Services
{
    public class AuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuthRepository _authRepository;
        private readonly ITokenService _tokenService;
        private readonly JWTSettings _jwtSettings;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            IAuthRepository authRepository,
            ITokenService tokenService,
            IOptions<JWTSettings> jwtSettings)
        {
            _userManager = userManager;
            _authRepository = authRepository;
            _tokenService = tokenService;
            _jwtSettings = jwtSettings.Value;
        }

        public async Task<AuthResponseDto> RegisterAsync(UserRegisterDto dto)
        {
            var existingUser = await _authRepository.GetByEmailAsync(dto.Email);
            if (existingUser is not null)
                throw new Exception("Email is already registered");

            var user = new ApplicationUser
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                UserName = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                throw new Exception(result.Errors.First().Description);

            await _userManager.AddToRoleAsync(user, "GymMember");

            var memberProfile = new MemberProfile
            {
                ApplicationUserId = user.Id
            };

            return await GenerateAuthResponseAsync(user);
        }

        public async Task<AuthResponseDto> LoginAsync(UserloginDto dto)
        {
            var user = await _authRepository.GetByEmailAsync(dto.Email);
            if (user is null || !await _userManager.CheckPasswordAsync(user, dto.Password))
                throw new Exception("Invalid email or password");

            if (!user.IsActive)
                throw new Exception("Account is deactivated");

            return await GenerateAuthResponseAsync(user);
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto dto)
        {
            var principal = _tokenService.GetPrincipalFromExpiredToken(dto.AccessToken);
            var userId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (userId is null)
                throw new Exception("Invalid token");

            var storedToken = await _authRepository.GetRefreshTokenAsync(dto.RefreshToken, userId);
            if (storedToken is null)
                throw new Exception("Invalid or expired refresh token");

            await _authRepository.RevokeRefreshTokenAsync(storedToken);

            var user = await _userManager.FindByIdAsync(userId);
            return await GenerateAuthResponseAsync(user!);
        }

        public async Task LogoutAsync(string userId)
        {
            await _authRepository.RevokeAllRefreshTokensAsync(userId);
        }

        public async Task<GetProfileDto> GetProfileAsync(string userId)
        {
            var user = await _authRepository.GetByIdWithProfileAsync(userId);
            if (user is null)
                throw new Exception("User not found");

            var activeSubscription = user.MemberProfile?.UserSubscriptions
                .FirstOrDefault(s => s.Status == "Active");

            return new UserProfileDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email!,
                PhoneNumber = user.PhoneNumber,
                PreferredLanguage = user.PreferredLanguage,
                Country = user.Country,
                IsActive = user.IsActive,
                Weight = user.MemberProfile?.Weight,
                Height = user.MemberProfile?.Height,
                BMI = user.MemberProfile?.BMI,
                Gender = user.MemberProfile?.Gender,
                ProfileImage = user.MemberProfile?.ProfileImage,
                ActiveSubscription = activeSubscription == null ? null : new UserSubscriptionDto
                {
                    PlanName = activeSubscription.SubscriptionPlan.Name,
                    StartDate = activeSubscription.StartDate,
                    EndDate = activeSubscription.EndDate,
                    Status = activeSubscription.Status,
                    RemainingSessions = activeSubscription.RemainingSessions
                }
            };
        }

        public async Task ChangePasswordAsync(string userId, ChangePasswordDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
                throw new Exception("User not found");

            var result = await _userManager.ChangePasswordAsync(
                user, dto.OldPassword, dto.NewPassword);

            if (!result.Succeeded)
                throw new Exception(result.Errors.First().Description);
        }

        public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            var user = await _authRepository.GetByEmailAsync(dto.Email);
            if (user is null) return;

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            // TODO: send token via email service
        }

        public async Task ResetPasswordAsync(ResetPasswordDto dto)
        {
            var user = await _authRepository.GetByEmailAsync(dto.Email);
            if (user is null)
                throw new Exception("User not found");

            var result = await _userManager.ResetPasswordAsync(
                user, dto.Token, dto.NewPassword);

            if (!result.Succeeded)
                throw new Exception(result.Errors.First().Description);
        }

        private async Task<AuthResponseDto> GenerateAuthResponseAsync(ApplicationUser user)
        {
            var accessToken = _tokenService.GenerateAccessToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            var token = new RefreshToken
            {
                Token = refreshToken,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays),
                IsRevoked = false
            };

            await _authRepository.SaveRefreshTokenAsync(token);

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes),
                Role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "GymMember"
            };
        }
    }
}
