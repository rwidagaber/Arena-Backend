using ArenaApi.Hubs;
using ArenaApi.ValidatorConfig;
using ArenaApplication.IServices;
using ArenaApplication.Services;
using ArenaDomain.Interfacees;
using ArenaInfrastructure;
using ArenaInfrastructure.Repositories;
namespace ArenaAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();
           

            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<INotificationHub, NotificationHubService>();
            builder.Services.ConfigureDbContext(builder.Configuration);
            builder.Services.AddValidators();
            builder.Services.AddSignalR();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseCors("AllowAll");

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();
            app.MapHub<NotificationHub>("/hubs/notifications");


            app.Run();
        }
    }
}
