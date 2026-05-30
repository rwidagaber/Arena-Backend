using ArenaApplication.Dtos.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ArenaApi.ValidatorConfig
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
