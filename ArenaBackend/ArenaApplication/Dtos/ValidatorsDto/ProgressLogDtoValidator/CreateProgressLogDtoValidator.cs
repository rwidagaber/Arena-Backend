using ArenaApplication.Dtos.ProgressLogDtos;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Dtos.ValidatorsDto.ProgressLogDtoValidator
{
    public class CreateProgressLogDtoValidator : AbstractValidator<CreateProgressLogDto>
    {
        public CreateProgressLogDtoValidator()
        {
            RuleFor(x => x.Weight)
                .GreaterThan(0)
                .WithMessage("Weight must be greater than 0")
                .LessThan(500)
                .WithMessage("Weight must be less than 500 kg");

            RuleFor(x => x.BodyFat)
                .InclusiveBetween(1, 100)
                .WithMessage("Body fat must be between 1 and 100")
                .When(x => x.BodyFat.HasValue);

            RuleFor(x => x.MuscleMass)
                .GreaterThan(0)
                .WithMessage("Muscle mass must be greater than 0")
                .When(x => x.MuscleMass.HasValue);

            RuleFor(x => x.LoggedAt)
                .NotEmpty()
                .WithMessage("Log date is required")
                .LessThanOrEqualTo(DateTime.UtcNow)
                .WithMessage("Log date cannot be in the future");
        }
    }
}
