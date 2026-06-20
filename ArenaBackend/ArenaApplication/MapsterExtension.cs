using Microsoft.Extensions.DependencyInjection;
using Mapster;
using MapsterMapper;

namespace ArenaApplication
{
    public static class MapsterExtension
    {
        public static IServiceCollection AddMapsterConfiguration(this IServiceCollection services)
        {
            // Configure Mapster TypeAdapterConfig and register the ServiceMapper so
            // MapsterMapper.IMapper will be resolvable from DI in any application that
            // calls this extension (API, MVC, etc.).
            var config = TypeAdapterConfig.GlobalSettings;

            // Scan mapping profiles located in the application assembly (adjust if needed)
            // This relies on mapping configs like AuthMappingConfig being in the same project.
            config.Scan(typeof(Mappers.AuthMappingConfig).Assembly);

            services.AddSingleton(config);
            services.AddScoped<IMapper, ServiceMapper>();

            return services;
        }
    }
}
