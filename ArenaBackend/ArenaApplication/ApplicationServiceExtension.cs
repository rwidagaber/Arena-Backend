using ArenaApplication.IServices;
using ArenaApplication.Services;
using ArenaApplication.Services.SubscriptionPlan;
using Microsoft.Extensions.DependencyInjection;

namespace ArenaApplication
{
    public static class ApplicationServiceExtension
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<ISubscriptionPlanService, SubscriptionPlanService>();
            services.AddScoped<IUserManagementService, UserManagementService>();
            return services;
        }
    }
}
