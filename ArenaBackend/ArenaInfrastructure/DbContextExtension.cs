using ArenaInfrastructure.AI;
using ArenaInfrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using ArenaDomain.Interfaces;
using ArenaInfrastructure.Repositories;
using Npgsql;
using Pgvector.Npgsql;

namespace ArenaInfrastructure
{
    public static class DbContextExtension
    {
        public static void ConfigureDbContext(this IServiceCollection services, IConfiguration configuration)
        {
            // ── SQL Server (existing) ────────────────────────────────────────────
            // All app data: users, workouts, nutrition, bookings, etc.
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString));

            // ── PostgreSQL + pgvector (Neon) ─────────────────────────────────────
            // Uses raw Npgsql ADO.NET (no EF Core provider) to avoid version issues.
            // NpgsqlDataSource is singleton — connections are pooled automatically.
            var vectorConnection = configuration.GetConnectionString("VectorConnection");

            if (!string.IsNullOrWhiteSpace(vectorConnection))
            {
                var dataSourceBuilder = new NpgsqlDataSourceBuilder(vectorConnection);
                dataSourceBuilder.UseVector(); // register pgvector type mappings
                services.AddSingleton(dataSourceBuilder.Build());
                services.AddScoped<NeonVectorStore>();
            }
            else
            {
                Console.WriteLine("[Config] ⚠️ VectorConnection not set — NeonVectorStore disabled");
            }

            // ── Repositories ────────────────────────────────────────────────────
            services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
        }
    }
}
