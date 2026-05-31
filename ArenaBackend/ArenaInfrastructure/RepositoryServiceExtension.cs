using ArenaDomain.Entities.Subscription;
using ArenaDomain.Interfacees;
using ArenaInfrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace ArenaInfrastructure
{
    public static class RepositoryServiceExtension
    {
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<IGenericRepository<SubscriptionPlan, Guid>, GenericRepository<SubscriptionPlan, Guid>>();
            return services;
        }
    }
}
