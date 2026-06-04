using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;
using ArenaApplication.Dtos.RegisterDto;
using System.ComponentModel.DataAnnotations;
using ArenaDomain.Shared;
using Microsoft.Extensions.Localization;

namespace ArenaApplication.Dtos.Validators
{
    public class RegisterDtoValidator : AbstractValidator<UserRegisterDto>
    {
        public RegisterDtoValidator(IStringLocalizer<ArenaLocalization> localizer) 
        {
            RuleFor(u => u.FirstName)
                .NotEmpty()
                .WithMessage(localizer["FirstNameRequired"])
                .MaximumLength(50)
                .WithMessage(localizer["NameMaxLength"]);

            RuleFor(u => u.LastName)
                .NotEmpty()
                .WithMessage(localizer["LastNameRequired"])
                .MaximumLength(50)
                .WithMessage(localizer["NameMaxLength"]);

            RuleFor(u => u.Email)
                .NotEmpty()
                .WithMessage(localizer["EmailRequired"])
                .EmailAddress()
                .WithMessage(localizer["ValidEmailRequired"]);

            RuleFor(u => u.Password)
                .NotEmpty()
                .MinimumLength(8)
                .WithMessage(localizer["PasswordMinLength"])
                .Matches("[A-z]")
                .WithMessage(localizer["PasswordUppercase"])
                .Matches("[a-z]")
                .WithMessage(localizer["PasswordLowercase"])
                .Matches("[0-9]")
                .WithMessage(localizer["PasswordNumber"])
                .Matches("[^a-zA-Z0-9]")
                .WithMessage(localizer["PasswordSpecialChar"]);

            RuleFor(u => u.ConfirmPassword)
                .NotEmpty()
                .WithMessage(localizer["ConfirmPasswordRequired"])
                .Equal(u => u.Password)
                .WithMessage(localizer["PasswordsDoNotMatch"]);

            RuleFor(u => u.PhoneNumber)
                .NotEmpty()
                .WithMessage(localizer["PhoneRequired"])
                .Matches(@"^\+?[0-9]{10,15}$")
                .WithMessage(localizer["ValidPhoneRequired"])
                .When(u => u.PhoneNumber != null);

            RuleFor(u => u.Birthday)
                .NotEmpty()
                .LessThan(DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-16)))
                .WithMessage(localizer["AgeRequirement"])
                .When(x => x.Birthday != default);
        }
    }
}
