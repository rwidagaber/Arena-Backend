using ArenaApplication.Services.SubscriptionPlan;
using Microsoft.Extensions.DependencyInjection;

namespace ArenaApplication
{
    public static class ApplicationServiceExtension
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<ISubscriptionPlanService, SubscriptionPlanService>();
            return services;
        }
    }
}
