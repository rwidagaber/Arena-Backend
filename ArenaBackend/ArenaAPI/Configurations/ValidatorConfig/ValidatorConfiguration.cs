using ArenaApplication.Dtos.Validators;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;

namespace ArenaApi.Configurations.ValidatorConfig
{
    public static class ValidatorConfiguration
    {
       
       public static IServiceCollection AddValidators(this IServiceCollection service)
       {
           service.AddFluentValidationAutoValidation();
           service.AddValidatorsFromAssemblyContaining<RegisterDtoValidator>();
           service.AddValidatorsFromAssemblyContaining<LoginDtoValidator>();

           return service;
       }


      
    }
}
