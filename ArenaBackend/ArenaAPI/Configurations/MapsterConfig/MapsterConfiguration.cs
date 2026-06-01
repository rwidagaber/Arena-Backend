using ArenaApplication.Mappers;
using Mapster;
using MapsterMapper;

namespace ArenaApi.Configurations.MapsterConfig
{
    public static class MapsterConfiguration
    {
        public static IServiceCollection AddMapster(
            this IServiceCollection services)
        {
            var config = TypeAdapterConfig.GlobalSettings;
            config.Scan(typeof(AuthMappingConfig).Assembly);

            services.AddSingleton(config);
            services.AddScoped<IMapper, ServiceMapper>();

            return services;
        }
    }
}
