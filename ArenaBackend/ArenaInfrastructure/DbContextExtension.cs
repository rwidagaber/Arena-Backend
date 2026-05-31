using ArenaInfrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using ArenaDomain.Interfacees;
using ArenaInfrastructure.Repositories;

namespace ArenaInfrastructure
{
    public static class DbContextExtension
    {
        public static void ConfigureDbContext(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString));

            // Register repositories
            services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));
            services.AddScoped<INotificationRepository, NotificationRepository>();

            services.AddScoped<IUnitOfWork, UnitOfWork>();
        }
    }
}
