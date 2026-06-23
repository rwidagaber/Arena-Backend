using ArenaDomain.Entities.Gym;
using ArenaDomain.Entities.Subscription;
using ArenaDomain.Entities.Nutrition;
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
            services.AddScoped<IGenericRepository<NutritionPlan, Guid>, GenericRepository<NutritionPlan, Guid>>();
            services.AddScoped<IGenericRepository<Meal, Guid>, GenericRepository<Meal, Guid>>();
            services.AddScoped<IGenericRepository<WorkingHours, int>, GenericRepository<WorkingHours, int>>();
            services.AddScoped<IGenericRepository<MealLog, Guid>, GenericRepository<MealLog, Guid>>();
            return services;
        }
    }
}
