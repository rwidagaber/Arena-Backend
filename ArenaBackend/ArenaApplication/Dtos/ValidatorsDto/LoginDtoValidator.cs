using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;
using ArenaApplication.Dtos.loginDto;


namespace ArenaApplication.Dtos.Validators
{
    public class LoginDtoValidator : AbstractValidator<UserloginDto>
    {
        public LoginDtoValidator() 
        {
            RuleFor(u => u.Email)
                .NotEmpty()
                .WithMessage("Email is required")
                .EmailAddress()
                .WithMessage("Enter a valid Email Format");


            RuleFor(u => u.Password)
                .NotEmpty()
                .WithMessage("Password is required");
               
        }
    }
}
