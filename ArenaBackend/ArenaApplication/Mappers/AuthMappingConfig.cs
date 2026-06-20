using ArenaApplication.Dtos.RegisterDto;
using ArenaDomain.Entities;
using ArenaDomain.Entities.User;
using Mapster;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Mappers
{
    public class AuthMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            // UserRegisterDto to ApplicationUser
            config.NewConfig<UserRegisterDto, ApplicationUser>()
                .Map(dest => dest.UserName, src => src.Email)
                .Map(dest => dest.Email, src => src.Email)
                .Map(dest => dest.FirstName, src => src.FirstName)
                .Map(dest => dest.LastName, src => src.LastName)
                .Map(dest => dest.PhoneNumber, src => src.PhoneNumber)
                .Map(dest => dest.PreferredLanguage, src => src.PreferredLanguage)
                .Map(dest => dest.IsActive, src => true)
                .Ignore(dest => dest.PasswordHash!);

            // UserRegisterDto to MemberProfile
            config.NewConfig<UserRegisterDto, MemberProfile>()
                .Map(dest => dest.DateOfBirth, src =>
                    src.Birthday.ToDateTime(TimeOnly.MinValue))
                .Ignore(dest => dest.Id)
                .Ignore(dest => dest.UserId)
                .Ignore(dest => dest.User);
        }
    }
}
