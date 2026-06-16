using System;
using System.Linq;
using System.Threading.Tasks;
using ArenaApplication.Dtos.ProfileDtos;
using ArenaApplication.Dtos.UserSupscriptionDto;
using ArenaApplication.IServices;
using ArenaDomain.Entities.User;
using ArenaDomain.Enums;
using ArenaDomain.Interfaces;
using ArenaDomain.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;

namespace ArenaApplication.Services
{
    public class ProfileService : IProfileService
    {
        private readonly IAuthRepository _authRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStringLocalizer<ArenaLocalization> _localizer;

        public ProfileService(
            IAuthRepository authRepository,
            UserManager<ApplicationUser> userManager,
            IStringLocalizer<ArenaLocalization> localizer)
        {
            _authRepository = authRepository;
            _userManager = userManager;
            _localizer = localizer;
        }

        public async Task<Result<GetProfileDto>> GetProfileAsync(Guid Id)
        {
            var user = await _authRepository.GetByIdWithProfileAsync(Id);
            if (user is null)
                return Result<GetProfileDto>.Failure(_localizer["UserNotFound"]);

            var activeSubscription = user.MemberProfile?.Subscriptions
                .FirstOrDefault(s => s.Status == SubscriptionStatus.Active);

            var isSubscribed = activeSubscription != null;

            var profile = new GetProfileDto
            {
                Id = user.Id,
                MemberProfileId = user.MemberProfile?.Id ?? Guid.Empty,
                FirstName = isSubscribed ? user.FirstName : _localizer["Locked"],
                LastName = isSubscribed ? user.LastName : _localizer["Locked"],
                Email = isSubscribed ? user.Email! : _localizer["Locked"],
                PhoneNumber = isSubscribed ? user.PhoneNumber : null,
                PreferredLanguage = isSubscribed ? user.PreferredLanguage : _localizer["Locked"],
                IsActive = isSubscribed ? user.IsActive : false,
                Weight = isSubscribed ? user.MemberProfile?.Weight : null,
                Height = isSubscribed ? user.MemberProfile?.Height : null,
                BMI = isSubscribed ? user.MemberProfile?.BMI : null,
                Gender = isSubscribed ? user.MemberProfile?.Gender.ToString() : null,
                ProfileImage = isSubscribed ? user.MemberProfile?.ProfileImageUrl : null,
                Birthday = isSubscribed && user.MemberProfile?.DateOfBirth != null
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
                    ReminderSent = activeSubscription.ReminderSent
                }
            };

            return Result<GetProfileDto>.Success(profile);
        }

        public async Task<Result<GetProfileDto>> UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
        {
            var user = await _authRepository.GetByIdWithProfileAsync(userId);
            if (user is null)
                return Result<GetProfileDto>.Failure(_localizer["UserNotFound"]);

            var activeSubscription = user.MemberProfile?.Subscriptions
                .FirstOrDefault(s => s.Status == SubscriptionStatus.Active);

            if (activeSubscription == null)
            {
                return Result<GetProfileDto>.Failure(_localizer["ProfileLockedRequiresSubscription"]);
            }

            if (dto.FirstName is not null)
                user.FirstName = dto.FirstName;

            if (dto.LastName is not null)
                user.LastName = dto.LastName;

            if (dto.PhoneNumber is not null)
                user.PhoneNumber = dto.PhoneNumber;

            if (dto.PreferredLanguage is not null)
                user.PreferredLanguage = dto.PreferredLanguage;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                return Result<GetProfileDto>.Failure(updateResult.Errors.Select(e => e.Description).ToArray());

            if (user.MemberProfile is not null)
            {
                if (dto.Weight is not null)
                    user.MemberProfile.Weight = dto.Weight;

                if (dto.Height is not null)
                    user.MemberProfile.Height = dto.Height;

                if (dto.Gender is not null && Enum.TryParse<Gender>(dto.Gender, out var gender))
                    user.MemberProfile.Gender = gender;

                if (dto.ProfileImage is not null)
                    user.MemberProfile.ProfileImageUrl = dto.ProfileImage;

                if (dto.Birthday is not null)
                    user.MemberProfile.DateOfBirth = dto.Birthday.Value.ToDateTime(TimeOnly.MinValue);

                if (dto.Weight is not null || dto.Height is not null)
                {
                    var weight = user.MemberProfile.Weight;
                    var height = user.MemberProfile.Height;

                    if (weight > 0 && height > 0)
                        user.MemberProfile.BMI = Math.Round(
                            weight.Value / ((height.Value / 100) * (height.Value / 100)), 2);
                }

                await _authRepository.UpdateMemberProfileAsync(user.MemberProfile);
            }

            var updatedProfile = new GetProfileDto
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
                                : null
            };

            return Result<GetProfileDto>.Success(updatedProfile);
        }

    }
}
