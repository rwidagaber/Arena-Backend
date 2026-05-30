using ArenaApplication.Dtos.ProfileDtos;
using ArenaApplication.Dtos.UserSupscriptionDto;
using ArenaApplication.IServices;
using ArenaDomain.Entities.User;
using ArenaDomain.Enums;
using ArenaDomain.Interfacees;
using ArenaDomain.Shared;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Services
{
    public class ProfileService :IProfileService
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

        public async Task<Result> UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
        {
            var user = await _authRepository.GetByIdWithProfileAsync(userId);
            if (user is null)
                return Result.Failure("User not found");

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
                return Result.Failure(updateResult.Errors.Select(e => e.Description).ToArray());

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

            return Result.Success();
        }
    }
}
