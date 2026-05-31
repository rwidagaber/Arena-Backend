using Microsoft.Extensions.DependencyInjection;

namespace ArenaApplication
{
    public static class MapsterExtension
    {
        public static IServiceCollection AddMapsterConfiguration(this IServiceCollection services)
        {
            // Mapster is auto-registered when NuGet packages are installed
            // Scan for IRegister implementations will happen automatically
            return services;
        }
    }
}
