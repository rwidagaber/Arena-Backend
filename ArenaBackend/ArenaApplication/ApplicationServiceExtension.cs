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
            services.AddScoped<IUserSubscriptionService, UserSubscriptionService>();
            return services;
        }
    }
}
