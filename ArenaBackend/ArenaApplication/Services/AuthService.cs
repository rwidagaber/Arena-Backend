using ArenaApplication.Dtos.AuthDtos;
using ArenaApplication.Dtos.AuthDtos.loginDto;
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
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System.Data;
using System.Security.Claims;
using System.Text;
using ArenaApplication.Dtos.AuthDtos.loginDto;


namespace ArenaApplication.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuthRepository _authRepository;
        private readonly ITokenService _tokenService;
        private readonly JWTSettings _jwtSettings;
        private readonly IOtpService _otpService;
        private readonly IBackgroundJobService _backgroundJobService;
        private readonly IGoogleTokenValidator _googleTokenValidator;
        private readonly IStringLocalizer<ArenaLocalization> _localizer;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService; // ✅ تمت الإضافة

        public AuthService(
            UserManager<ApplicationUser> userManager,
            IAuthRepository authRepository,
            ITokenService tokenService,
            IBackgroundJobService backgroundJobService,
            IOtpService otpService,
            IOptions<JWTSettings> jwtSettings,
            IStringLocalizer<ArenaLocalization> localizer,
            IGoogleTokenValidator googleTokenValidator,
            INotificationService notificationService,
            IEmailService emailService) // ✅ تمت الإضافة
        {
            _userManager = userManager;
            _authRepository = authRepository;
            _tokenService = tokenService;
            _jwtSettings = jwtSettings.Value;
            _backgroundJobService = backgroundJobService;
            _otpService = otpService;
            _localizer = localizer;
            _googleTokenValidator = googleTokenValidator;
            _notificationService = notificationService;
            _emailService = emailService; // ✅ تمت الإضافة
        }

        // =========================
        // REGISTER
        // =========================

        public async Task<Result<Guid>> RegisterAsync(UserRegisterDto dto)
        {
            var existingUser = await _authRepository.GetByEmailAsync(dto.Email);
            if (existingUser is not null)
                return Result<Guid>.Failure("Email is already registered");

            var user = new ApplicationUser
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                UserName = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                IsActive = false,
                EmailConfirmed = false
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return Result<Guid>.Failure(result.Errors.Select(e => e.Description).ToArray());

            try
            {
                var memberProfile = new MemberProfile
                {
                    UserId = user.Id,
                    Weight = dto.Weight,
                    Height = dto.Height,
                };

                await _authRepository.CreateMemberProfileAsync(memberProfile);

                var otp = await _otpService.GenerateAndSaveOtpAsync(user.Id);

                await _backgroundJobService.EnqueueEmailConfirmationAsync(
                    user.Id,
                    user.Email!,
                    otp
                );

                return Result<Guid>.Success(user.Id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Register failed: {ex.Message}");
                Console.WriteLine($"❌ Inner: {ex.InnerException?.Message}");

                await _userManager.DeleteAsync(user);
                return Result<Guid>.Failure("Failed to create user profile");
            }
        }

        public async Task<Result<AuthResponseDto>> ConfirmEmailAsync(ConfirmEmailDto dto)
        {
            var user = await _userManager.FindByIdAsync(dto.UserId.ToString());
            if (user is null)
                return Result<AuthResponseDto>.Failure("User not found");

            if (user.EmailConfirmed)
                return Result<AuthResponseDto>.Failure("Email is already confirmed");

            var isValid = await _otpService.ValidateOtpAsync(dto.UserId, dto.Otp);
            if (!isValid)
                return Result<AuthResponseDto>.Failure("Invalid or expired OTP");

            user.IsActive = true;
            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);

            await _userManager.AddToRoleAsync(user, "GymMember");

            var userWithProfile = await _authRepository.GetByIdWithProfileAsync(user.Id);
            if (userWithProfile?.MemberProfile != null)
            {
                await _notificationService.NotifyWelcomeAsync(
                    userWithProfile.MemberProfile.Id,
                    user.FirstName);
            }

            var response = await GenerateAuthResponseAsync(user);
            return Result<AuthResponseDto>.Success(response);
        }

        // ── LoginAsync ────────────────────────────────────────────
        public async Task<Result<AuthResponseDto>> LoginAsync(UserloginDto dto)
        {
            var user = await _authRepository.GetByEmailAsync(dto.Email);

            if (user is null)
                return Result<AuthResponseDto>.Failure(_localizer["InvalidEmailOrPassword"]);

            if (user.IsGoogleAccount && !await _userManager.HasPasswordAsync(user))
                return Result<AuthResponseDto>.Failure("GOOGLE_ACCOUNT_ONLY");

            if (!await _userManager.CheckPasswordAsync(user, dto.Password))
                return Result<AuthResponseDto>.Failure(_localizer["InvalidEmailOrPassword"]);

            if (!user.IsActive)
                return Result<AuthResponseDto>.Failure(_localizer["AccountIsDeactivated"]);

            if (user.MemberProfile is null)
                user = await _authRepository.GetByIdWithProfileAsync(user.Id) ?? user;

            var response = await GenerateAuthResponseAsync(user);

            var activeSubscription = user.MemberProfile?.Subscriptions
                .Where(s => s.Status == SubscriptionStatus.Active)
                .OrderByDescending(s => s.Plan?.HasAI == true)
                .FirstOrDefault();
            response.IsSubscribed = activeSubscription is not null;

            return Result<AuthResponseDto>.Success(response);
        }

        public async Task<Result<AuthResponseDto>> RefreshTokenAsync(RefreshTokenDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.AccessToken) || string.IsNullOrWhiteSpace(dto.RefreshToken))
                return Result<AuthResponseDto>.Failure(_localizer["InvalidToken"]);

            ClaimsPrincipal principal;
            try
            {
                principal = _tokenService.GetPrincipalFromExpiredToken(dto.AccessToken);
            }
            catch (Exception)
            {
                return Result<AuthResponseDto>.Failure(_localizer["InvalidToken"]);
            }

            var userIdStr = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userIdStr is null)
                return Result<AuthResponseDto>.Failure(_localizer["InvalidToken"]);

            var userId = Guid.Parse(userIdStr);

            var storedToken = await _authRepository.GetRefreshTokenAsync(dto.RefreshToken, userId);
            if (storedToken is null)
                return Result<AuthResponseDto>.Failure(_localizer["InvalidOrExpiredRefreshToken"]);

            await _authRepository.RevokeRefreshTokenAsync(storedToken);

            var user = await _authRepository.GetByIdWithProfileAsync(userId);
            if (user is null)
                return Result<AuthResponseDto>.Failure(_localizer["UserNotFound"]);

            var response = await GenerateAuthResponseAsync(user);

            var activeSubscription = user.MemberProfile?.Subscriptions
                .Where(s => s.Status == SubscriptionStatus.Active)
                .OrderByDescending(s => s.Plan?.HasAI == true)
                .FirstOrDefault();
            response.IsSubscribed = activeSubscription is not null;

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
                return Result<GetProfileDto>.Failure(_localizer["UserNotFound"]);

            var activeSubscription = user.MemberProfile?.Subscriptions
                .Where(s => s.Status == SubscriptionStatus.Active)
                .OrderByDescending(s => s.Plan?.HasAI == true)
                .FirstOrDefault();

            var profile = new GetProfileDto
            {
                Id = user.Id,
                MemberProfileId = user.MemberProfile?.Id ?? Guid.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email!,
                PhoneNumber = user.PhoneNumber,
                PreferredLanguage = user.PreferredLanguage,
                IsActive = user.IsActive,
                Weight = user.MemberProfile?.Weight,
                Height = user.MemberProfile?.Height,
                BMI = user.MemberProfile?.BMI,
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
                    Status = activeSubscription.Status.ToString(),
                    RemainingSessions = activeSubscription.RemainingSessions,
                    TotalSessions = activeSubscription.Plan?.SessionLimit ?? 0,
                    PaymentAmount = activeSubscription.Plan?.Price ?? 0,
                    ReminderSent = activeSubscription.ReminderSent,
                    HasAI = activeSubscription.Plan?.HasAI ?? false
                }
            };

            return Result<GetProfileDto>.Success(profile);
        }

        public async Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                return Result.Failure(_localizer["UserNotFound"]);

            var result = await _userManager.ChangePasswordAsync(
                user, dto.OldPassword, dto.NewPassword);

            if (!result.Succeeded)
                return Result.Failure(result.Errors.Select(e => e.Description).ToArray());

            return Result.Success();
        }

        public async Task<Result> DeleteAccountAsync(Guid userId, DeleteAccountDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null || user.IsDeleted)
                return Result.Failure(_localizer["UserNotFound"]);

            // Verify the password before destroying the account. Google accounts
            // that never set a password are the only ones exempt from this check.
            var hasPassword = await _userManager.HasPasswordAsync(user);
            if (hasPassword && !await _userManager.CheckPasswordAsync(user, dto.Password))
                return Result.Failure(_localizer["IncorrectPassword"]);

            // Soft delete: keep the row (and its FKs) intact but lock the account
            // out. Login already rejects users with IsActive == false.
            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return Result.Failure(result.Errors.Select(e => e.Description).ToArray());

            // Kill every active session so the (now deleted) account can't keep using tokens.
            await _authRepository.RevokeAllRefreshTokensAsync(userId);

            return Result.Success();
        }

        public async Task<Result> ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            var user = await _authRepository.GetByEmailAsync(dto.Email);

            if (user is null)
                return Result.Success();

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

            await _emailService.SendPasswordResetTokenAsync(user.Email!, resetToken, dto.Email);

            return Result.Success();
        }

        // ── ResetPasswordAsync ────────────────────────────────────
        public async Task<Result> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var user = await _authRepository.GetByEmailAsync(dto.Email);
            if (user is null)
                return Result.Failure(_localizer["UserNotFound"]);

            try
            {
                var decodedToken = Encoding.UTF8.GetString(
                    WebEncoders.Base64UrlDecode(dto.Token));

                IdentityResult result;

                if (user.IsGoogleAccount && !await _userManager.HasPasswordAsync(user))
                {
                    result = await _userManager.AddPasswordAsync(user, dto.NewPassword);
                }
                else
                {
                    result = await _userManager.ResetPasswordAsync(
                        user, decodedToken, dto.NewPassword);
                }

                if (!result.Succeeded)
                    return Result.Failure(result.Errors.Select(e => e.Description).ToArray());

                return Result.Success();
            }
            catch (FormatException)
            {
                return Result.Failure("Invalid or malformed reset token.");
            }
        }

        private async Task<AuthResponseDto> GenerateAuthResponseAsync(ApplicationUser user, bool isGoogleUser = false)
        {
            var userWithProfile = await _authRepository.GetByIdWithProfileAsync(user.Id);

            var accessToken = await _tokenService.GenerateAccessToken(userWithProfile ?? user);
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
                Role = role,
                IsGoogleUser = isGoogleUser
            };
        }

        // ── GoogleLoginAsync ──────────────────────────────────────
        public async Task<Result<AuthResponseDto>> GoogleLoginAsync(string idToken)
        {
            var googleUser = await _googleTokenValidator.ValidateAsync(idToken);
            if (googleUser is null)
                return Result<AuthResponseDto>.Failure("Invalid Google token");

            var email = googleUser.Email;
            var firstName = googleUser.FirstName;
            var lastName = googleUser.LastName;

            var user = await _authRepository.GetByEmailAsync(email);

            if (user is null)
            {
                user = new ApplicationUser
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    UserName = email,
                    IsActive = true,
                    EmailConfirmed = true,
                    IsGoogleAccount = true
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                    return Result<AuthResponseDto>.Failure(
                        createResult.Errors.Select(e => e.Description).ToArray());

                var memberProfile = new MemberProfile { UserId = user.Id };
                await _authRepository.CreateMemberProfileAsync(memberProfile);
                await _userManager.AddToRoleAsync(user, "GymMember");
                var newUserWithProfile = await _authRepository.GetByIdWithProfileAsync(user.Id);
                if (newUserWithProfile?.MemberProfile != null)
                {
                    await _notificationService.NotifyWelcomeAsync(
                        newUserWithProfile.MemberProfile.Id,
                        firstName);
                }

                var newUserResponse = await GenerateAuthResponseAsync(user, isGoogleUser: true);
                return Result<AuthResponseDto>.Success(newUserResponse);
            }

            if (!user.IsGoogleAccount)
            {
                user.IsGoogleAccount = true;
                user.EmailConfirmed = true;
                user.IsActive = true;
                await _userManager.UpdateAsync(user);
            }

            var response = await GenerateAuthResponseAsync(user, isGoogleUser: false);

            var activeSubscription = user.MemberProfile?.Subscriptions
                .Where(s => s.Status == SubscriptionStatus.Active)
                .OrderByDescending(s => s.Plan?.HasAI == true)
                .FirstOrDefault();
            response.IsSubscribed = activeSubscription is not null;

            return Result<AuthResponseDto>.Success(response);
        }

        public async Task<Result> CompleteProfileAsync(Guid userId, CompleteProfileDto dto)
        {
            var user = await _authRepository.GetByIdWithProfileAsync(userId);
            if (user is null)
                return Result.Failure(_localizer["UserNotFound"]);

            user.PhoneNumber = dto.PhoneNumber;
            await _userManager.UpdateAsync(user);

            if (user.MemberProfile is null)
                return Result.Failure("Profile not found");

            user.MemberProfile.DateOfBirth = dto.DateOfBirth;
            user.MemberProfile.Weight = dto.Weight;
            user.MemberProfile.Height = dto.Height;
            user.MemberProfile.Gender = dto.Gender;

            if (dto.Height > 0)
            {
                var heightInMeters = dto.Height / 100;
                user.MemberProfile.BMI = Math.Round(
                    dto.Weight / (heightInMeters * heightInMeters), 1);
            }

            await _authRepository.UpdateMemberProfileAsync(user.MemberProfile);

            return Result.Success();
        }

        public async Task<Result> ResendConfirmationAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                return Result.Failure("User not found");

            if (user.EmailConfirmed)
                return Result.Failure("Email is already confirmed");

            var otp = await _otpService.GenerateAndSaveOtpAsync(user.Id);

            await _backgroundJobService.EnqueueEmailConfirmationAsync(
                user.Id,
                user.Email!,
                otp
            );

            return Result.Success();
        }
    }
}