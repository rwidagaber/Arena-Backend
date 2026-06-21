using ArenaApplication.IServices;
using ArenaApplication.Services;
using ArenaApplication.Services.SubscriptionPlan;
using ArenaApplication.Services.UserSubscription;
using Microsoft.Extensions.DependencyInjection;

namespace ArenaApplication
{
    public static class ApplicationServiceExtension
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<ISubscriptionPlanService, SubscriptionPlanService>();
            services.AddScoped<IUserManagementService, UserManagementService>();
            services.AddScoped<IUserSubscriptionService, UserSubscriptionService>();
            services.AddScoped<IWorkoutPlanService, WorkoutPlanService>();
            services.AddScoped<INutritionPlanService, NutritionPlanService>();
            services.AddScoped<IMealLogService, MealLogService>();
            return services;
        }
    }
}
