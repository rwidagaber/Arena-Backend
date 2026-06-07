using ArenaApplication.Dtos.ProfileDtos;
using ArenaApplication.Dtos.UserSupscriptionDto;
using ArenaDomain.Entities;
using ArenaDomain.Entities.User;
using ArenaDomain.Enums;
using Mapster;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Mappers
{
    public class ProfileMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {

            // ApplicationUser to GetProfileDto
            config.NewConfig<ApplicationUser, GetProfileDto>()
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.FirstName, src => src.FirstName)
                .Map(dest => dest.LastName, src => src.LastName)
                .Map(dest => dest.Email, src => src.Email)
                .Map(dest => dest.PhoneNumber, src => src.PhoneNumber)
                .Map(dest => dest.PreferredLanguage, src => src.PreferredLanguage)
                .Map(dest => dest.IsActive, src => src.IsActive)
                .Map(dest => dest.Weight, src => (double?)src.MemberProfile!.Weight)
                .Map(dest => dest.Height, src => (double?)src.MemberProfile!.Height)
                .Map(dest => dest.BMI, src => (double?)src.MemberProfile!.BMI)
                .Map(dest => dest.Gender, src => src.MemberProfile!.Gender.ToString())
                .Map(dest => dest.ProfileImage, src => src.MemberProfile!.ProfileImageUrl)
                .Map(dest => dest.Birthday, src =>
                    src.MemberProfile != null
                    ? DateOnly.FromDateTime(src.MemberProfile.DateOfBirth)
                    : (DateOnly?)null)
                .Map(dest => dest.ActiveSubscription, src =>
                    src.MemberProfile!.Subscriptions
                        .FirstOrDefault(s => s.Status == SubscriptionStatus.Active)
                        .Adapt<UserSubscriptionDto>());

            // UpdateProfileDto to ApplicationUser
            config.NewConfig<UpdateProfileDto, ApplicationUser>()
                .Map(dest => dest.FirstName, src => src.FirstName)
                .Map(dest => dest.LastName, src => src.LastName)
                .Map(dest => dest.PhoneNumber, src => src.PhoneNumber)
                .Map(dest => dest.PreferredLanguage, src => src.PreferredLanguage)
                .IgnoreNullValues(true);

            // UpdateProfileDto to MemberProfile
            config.NewConfig<UpdateProfileDto, MemberProfile>()
                .Map(dest => dest.Weight, src => src.Weight)
                .Map(dest => dest.Height, src => src.Height)
                .Map(dest => dest.Gender, src => Enum.Parse<Gender>(src.Gender!))
                .Map(dest => dest.ProfileImageUrl, src => src.ProfileImage)
                .Map(dest => dest.DateOfBirth, src =>
                    src.Birthday.HasValue
                    ? src.Birthday.Value.ToDateTime(TimeOnly.MinValue)
                    : (DateTime?)null)
                .IgnoreNullValues(true);
        }
    }
}
