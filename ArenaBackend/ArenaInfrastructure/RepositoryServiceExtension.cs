using ArenaDomain.Entities.Subscription;
using ArenaDomain.Entities.Workout;
using ArenaDomain.Interfaces;
using ArenaInfrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace ArenaInfrastructure
{
    public static class RepositoryServiceExtension
    {
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<IGenericRepository<SubscriptionPlan, Guid>, GenericRepository<SubscriptionPlan, Guid>>();
            services.AddScoped<IGenericRepository<UserSubscription, Guid>, GenericRepository<UserSubscription, Guid>>();
            services.AddScoped<IGenericRepository<WorkoutPlan, Guid>, GenericRepository<WorkoutPlan, Guid>>();
            return services;
        }
    }
}
