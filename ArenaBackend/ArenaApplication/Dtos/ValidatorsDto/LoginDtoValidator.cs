using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;
using ArenaApplication.Dtos.loginDto;
using ArenaDomain.Shared;
using Microsoft.Extensions.Localization;

namespace ArenaApplication.Dtos.Validators
{
    public class LoginDtoValidator : AbstractValidator<UserloginDto>
    {
        public LoginDtoValidator(IStringLocalizer<ArenaLocalization> localizer) 
        {
            RuleFor(u => u.Email)
                .NotEmpty()
                .WithMessage(localizer["EmailRequired"])
                .EmailAddress()
                .WithMessage(localizer["ValidEmailRequired"]);

            RuleFor(u => u.Password)
                .NotEmpty()
                .WithMessage(localizer["PasswordRequired"]);
        }
    }
}
