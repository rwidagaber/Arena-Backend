using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;
using ArenaApplication.Dtos.RegisterDto;
using System.ComponentModel.DataAnnotations;



namespace ArenaApplication.Dtos.Validators
{
    public class RegisterDtoValidator : AbstractValidator<UserRegisterDto>
    {
        public RegisterDtoValidator() 
        {
            RuleFor(u => u.FirstName)
                .NotEmpty()
                .WithMessage("First Name is Required")
                .MaximumLength(50)
                .WithMessage("Name must be less than 50 Characters");



            RuleFor(u => u.LastName)
                .NotEmpty()
                .WithMessage("Last Name is Required")
                .MaximumLength(50)
                .WithMessage("Name must be less than 50 characters");



            RuleFor(u => u.Email)
                .NotEmpty()
                .WithMessage("Email is required")
                .EmailAddress()
                .WithMessage("Enter a valid Email Address");



            RuleFor(u => u.Password)
                .NotEmpty()
                .MinimumLength(8)
                .WithMessage("Password must be 8 characters or more")
                .Matches("[A-z]")
                .WithMessage("password must contain uppercase characters")
                .Matches("[a-z]")
                .WithMessage("password must contain lowercase characters")
                .Matches("[0-9]")
                .WithMessage("password must contain at least one numbers")
                .Matches("[^a-zA-Z0-9]")
                .WithMessage("password must contain at least one speacial character");



            RuleFor(u => u.ConfirmPassword)
                .NotEmpty()
                .WithMessage("confirm password is required")
                .Equal(u => u.Password)
                .WithMessage("passwords don't match");

            RuleFor(u => u.PhoneNumber)
                .NotEmpty()
                .WithMessage("Phone Number is required")
                .Matches(@"^\+?[0-9]{10,15}$")
                .WithMessage("Enter a valid phone Number")
                .When(u => u.PhoneNumber != null);



            RuleFor(u => u.Birthday)
                .NotEmpty()
                .LessThan(DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-16)))
                .WithMessage("User must be 16 years or older")
                .When(x => x.Birthday != default);



        }
    }
}
