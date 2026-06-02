using System;
using System.Linq;
using System.Threading.Tasks;
using ArenaApplication.Dtos.ProfileDtos;
using ArenaApplication.Dtos.UserSupscriptionDto;
using ArenaApplication.IServices;
using ArenaDomain.Entities.User;
using ArenaDomain.Enums;
using ArenaDomain.Interfacees;
using ArenaDomain.Shared;
using Microsoft.AspNetCore.Identity;

namespace ArenaApplication.Services
{
    public class ProfileService : IProfileService
    {
        private readonly IAuthRepository _authRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileService(
            IAuthRepository authRepository,
            UserManager<ApplicationUser> userManager)
        {
            _authRepository = authRepository;
            _userManager = userManager;
        }

        public async Task<Result<GetProfileDto>> GetProfileAsync(Guid Id)
        {
            var user = await _authRepository.GetByIdWithProfileAsync(Id);
            if (user is null)
                return Result<GetProfileDto>.Failure("User not found");

            var activeSubscription = user.MemberProfile?.Subscriptions
                .FirstOrDefault(s => s.Status == SubscriptionStatus.Active);

            var isSubscribed = activeSubscription != null;

            var profile = new GetProfileDto
            {
                Id = user.Id,
                MemberProfileId = user.MemberProfile?.Id ?? Guid.Empty,
                // If user is not subscribed, lock profile fields by redacting them.
                FirstName = isSubscribed ? user.FirstName : "Locked",
                LastName = isSubscribed ? user.LastName : "Locked",
                Email = isSubscribed ? user.Email! : "Locked",
                PhoneNumber = isSubscribed ? user.PhoneNumber : null,
                PreferredLanguage = isSubscribed ? user.PreferredLanguage : "Locked",
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
                    Status = activeSubscription.Status,
                    RemainingSessions = activeSubscription.RemainingSessions,
                    ReminderSent = activeSubscription.ReminderSent
                }
            };

            return Result<GetProfileDto>.Success(profile);
        }

        public async Task<Result<GetProfileDto>> UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
        {
            var user = await _authRepository.GetByIdWithProfileAsync(userId);
            if (user is null)
                return Result<GetProfileDto>.Failure("User not found");

            // Check subscription first - disallow updates if no active subscription
            var activeSubscription = user.MemberProfile?.Subscriptions
                .FirstOrDefault(s => s.Status == SubscriptionStatus.Active);

            if (activeSubscription == null)
            {
                return Result<GetProfileDto>.Failure("Profile is locked. Active subscription required to update profile.");
            }

            // Update ApplicationUser fields
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

            // Update MemberProfile fields
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

                // Recalculate BMI if weight or height updated
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

            // ✅ Build the updated DTO to return
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