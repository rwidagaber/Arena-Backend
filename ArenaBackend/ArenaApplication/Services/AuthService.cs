using ArenaApplication.Dtos.AuthDtos;
using ArenaApplication.Dtos.loginDto;
using ArenaApplication.Dtos.ProfileDtos;
using ArenaApplication.Dtos.RegisterDto;
using ArenaApplication.Dtos.UserSupscriptionDto;
using ArenaApplication.IServices;
using ArenaDomain.Entities;
using ArenaDomain.Entities.User;
using ArenaDomain.Enums;
using ArenaDomain.Interfaces;
using ArenaDomain.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace ArenaApplication.Services
{
    public class AuthService : IAuthService
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

        public async Task<Result<AuthResponseDto>> RegisterAsync(UserRegisterDto dto)
        {
            var existingUser = await _authRepository.GetByEmailAsync(dto.Email);
            if (existingUser is not null)
                return Result<AuthResponseDto>.Failure("Email is already registered");

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
                return Result<AuthResponseDto>.Failure(
                    result.Errors.Select(e => e.Description).ToArray());

            await _userManager.AddToRoleAsync(user, "GymMember");

            var memberProfile = new MemberProfile
            {
                UserId = user.Id,
                DateOfBirth = dto.Birthday.ToDateTime(TimeOnly.MinValue)
            };

            await _authRepository.CreateMemberProfileAsync(memberProfile);
            user.MemberProfile = memberProfile;


            var response = await GenerateAuthResponseAsync(user);
            return Result<AuthResponseDto>.Success(response);
        }

        public async Task<Result<AuthResponseDto>> LoginAsync(UserloginDto dto)
        {
            var user = await _authRepository.GetByEmailAsync(dto.Email);
            if (user is null || !await _userManager.CheckPasswordAsync(user, dto.Password))
                return Result<AuthResponseDto>.Failure("Invalid email or password");

            if (!user.IsActive)
                return Result<AuthResponseDto>.Failure("Account is deactivated");
            if (user.MemberProfile is null)
                user = await _authRepository.GetByIdWithProfileAsync(user.Id) ?? user;

            var response = await GenerateAuthResponseAsync(user);
            return Result<AuthResponseDto>.Success(response);
        }

        public async Task<Result<AuthResponseDto>> RefreshTokenAsync(RefreshTokenDto dto)
        {
            var principal = _tokenService.GetPrincipalFromExpiredToken(dto.AccessToken);
            var userIdStr = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userIdStr is null)
                return Result<AuthResponseDto>.Failure("Invalid token");

            var userId = Guid.Parse(userIdStr);

            var storedToken = await _authRepository.GetRefreshTokenAsync(dto.RefreshToken, userId);
            if (storedToken is null)
                return Result<AuthResponseDto>.Failure("Invalid or expired refresh token");

            await _authRepository.RevokeRefreshTokenAsync(storedToken);

           
            var user = await _authRepository.GetByIdWithProfileAsync(userId);
            if (user is null)
                return Result<AuthResponseDto>.Failure("User not found");
            var response = await GenerateAuthResponseAsync(user);
            return Result<AuthResponseDto>.Success(response);
        }

        public async Task<Result> LogoutAsync(Guid userId)
        {
            await _authRepository.RevokeAllRefreshTokensAsync(userId);
            return Result.Success();
        }

        public async Task<Result<GetProfileDto>> GetProfileAsync(Guid userId)
        {
            var user = await _authRepository.GetByIdWithProfileAsync(userId);
            if (user is null)
                return Result<GetProfileDto>.Failure("User not found");

            var activeSubscription = user.MemberProfile?.Subscriptions
                .FirstOrDefault(s => s.Status == SubscriptionStatus.Active);

            var profile = new GetProfileDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email!,
                PhoneNumber = user.PhoneNumber,
                PreferredLanguage = user.PreferredLanguage,
                IsActive = user.IsActive,
                Weight = (decimal?)user.MemberProfile?.Weight,
                Height = (decimal?)user.MemberProfile?.Height,
                BMI = (decimal?)user.MemberProfile?.BMI,
                Gender = user.MemberProfile?.Gender.ToString(),
                ProfileImage = user.MemberProfile?.ProfileImageUrl,
                Birthday = user.MemberProfile?.DateOfBirth != null
                                    ? DateOnly.FromDateTime(user.MemberProfile.DateOfBirth)
                                    : null,
                ActiveSubscription = activeSubscription == null ? null : new UserSubscriptionDto
                {
                    Id = activeSubscription.Id,
                    PlanNameEn = activeSubscription.Plan.NameEn,
                    PlanNameAr = activeSubscription.Plan.NameAr,
                    StartDate = activeSubscription.StartDate,
                    EndDate = activeSubscription.EndDate,
                    Status = activeSubscription.Status,
                    RemainingSessions = activeSubscription.RemainingSessions,
                    ReminderSent = activeSubscription.ReminderSent
                }
            };

            return Result<GetProfileDto>.Success(profile);
        }

        public async Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                return Result.Failure("User not found");

            var result = await _userManager.ChangePasswordAsync(
                user, dto.OldPassword, dto.NewPassword);

            if (!result.Succeeded)
                return Result.Failure(result.Errors.Select(e => e.Description).ToArray());

            return Result.Success();
        }

        public async Task<Result> ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            var user = await _authRepository.GetByEmailAsync(dto.Email);
            if (user is null)
                return Result.Success();

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

            return Result.Success();
        }

        public async Task<Result> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var user = await _authRepository.GetByEmailAsync(dto.Email);
            if (user is null)
                return Result.Failure("User not found");

            var result = await _userManager.ResetPasswordAsync(
                user, dto.Token, dto.NewPassword);

            if (!result.Succeeded)
                return Result.Failure(result.Errors.Select(e => e.Description).ToArray());

            return Result.Success();
        }

        private async Task<AuthResponseDto> GenerateAuthResponseAsync(ApplicationUser user)
        {
            var accessToken = await _tokenService.GenerateAccessToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            var token = new RefreshToken
            {
                Token = refreshToken,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays),
                IsRevoked = false
            };

            await _authRepository.SaveRefreshTokenAsync(token);

            var role = (await _userManager.GetRolesAsync(user))
                .FirstOrDefault() ?? "GymMember";

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes),
                Role = role
            };
        }
    
    }
}
