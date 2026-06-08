using ArenaApplication.Dtos.AuthDtos;
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
using Microsoft.AspNetCore.WebUtilities;
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

        public AuthService(
            UserManager<ApplicationUser> userManager,
            IAuthRepository authRepository,
            ITokenService tokenService,
            IBackgroundJobService backgroundJobService,
            IOtpService otpService,
            IOptions<JWTSettings> jwtSettings,
            IStringLocalizer<ArenaLocalization> localizer,
             IGoogleTokenValidator googleTokenValidator)
        {
            _userManager = userManager;
            _authRepository = authRepository;
            _tokenService = tokenService;
            _jwtSettings = jwtSettings.Value;
            _backgroundJobService = backgroundJobService;
            _otpService = otpService;
            _localizer = localizer;
            _googleTokenValidator = googleTokenValidator;

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

                return Result<Guid>.Success(user.Id); // ← رجّع الـ userId
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Register failed: {ex.Message}");
                Console.WriteLine($"❌ Inner: {ex.InnerException?.Message}");

                await _userManager.DeleteAsync(user);
                return Result<Guid>.Failure("Failed to create user profile");
            }
        }

        // ✅ الميثود الجديدة — بتأكد الإيميل وترجع tokens مباشرة
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

            // ✅ نكمل الـ setup بعد التأكيد
            user.IsActive = true;
            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);

            await _userManager.AddToRoleAsync(user, "GymMember");

            var response = await GenerateAuthResponseAsync(user);
            return Result<AuthResponseDto>.Success(response);
        }





        public async Task<Result<AuthResponseDto>> LoginAsync(UserloginDto dto)
        {
            var user = await _authRepository.GetByEmailAsync(dto.Email);

            // لو اتسجل بـ Google مش هيقدر يعمل login عادي
            if (user?.IsGoogleAccount == true)
                return Result<AuthResponseDto>.Failure("This account uses Google Sign-In. Please login with Google.");

            if (user is null || !await _userManager.CheckPasswordAsync(user, dto.Password))
                return Result<AuthResponseDto>.Failure(_localizer["InvalidEmailOrPassword"]);

            if (!user.IsActive)
                return Result<AuthResponseDto>.Failure(_localizer["AccountIsDeactivated"]);

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
                return Result.Failure(_localizer["UserNotFound"]);

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

            // مش محتاج OTP خالص
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

            await _backgroundJobService.EnqueuePasswordResetTokenEmailAsync(
                user.Email!,
                resetToken,
                dto.Email);

            return Result.Success();
        }



        public async Task<Result> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var user = await _authRepository.GetByEmailAsync(dto.Email);
            if (user is null)
                return Result.Failure(_localizer["UserNotFound"]);

            try
            {
                // Decode الـ token
                var decodedToken = Encoding.UTF8.GetString(
                    WebEncoders.Base64UrlDecode(dto.Token));

                var result = await _userManager.ResetPasswordAsync(
                    user, decodedToken, dto.NewPassword);

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

                // يوديه يكمل البروفايل
                var newUserResponse = await GenerateAuthResponseAsync(user, isGoogleUser: true);
                return Result<AuthResponseDto>.Success(newUserResponse);
            }

            // يوزر موجود — Login عادي
            var response = await GenerateAuthResponseAsync(user, isGoogleUser: false);
            return Result<AuthResponseDto>.Success(response);
        }

        public async Task<Result> CompleteProfileAsync(Guid userId, CompleteProfileDto dto)
        {
            var user = await _authRepository.GetByIdWithProfileAsync(userId);
            if (user is null)
                return Result.Failure(_localizer["UserNotFound"]);

            // تحديث الـ phone
            user.PhoneNumber = dto.PhoneNumber;
            await _userManager.UpdateAsync(user);

            if (user.MemberProfile is null)
                return Result.Failure("Profile not found");

            // تحديث الـ profile
            user.MemberProfile.DateOfBirth = dto.DateOfBirth;
            user.MemberProfile.Weight = dto.Weight;
            user.MemberProfile.Height = dto.Height;
            user.MemberProfile.Gender = dto.Gender;

            // حساب الـ BMI
            if (dto.Height > 0)
            {
                var heightInMeters = dto.Height / 100;
                user.MemberProfile.BMI = Math.Round(
                    dto.Weight / (heightInMeters * heightInMeters), 1);
            }

            await _authRepository.UpdateMemberProfileAsync(user.MemberProfile);

            return Result.Success();
        }
    }
}
