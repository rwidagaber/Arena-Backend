using ArenaApi.Configurations;
using ArenaApi.Configurations.BrearerConfig;
using ArenaApi.Configurations.JWTConfig;
using ArenaApi.Configurations.ValidatorConfig;
using ArenaApi.Hubs;
using ArenaApplication;
using ArenaApplication.IServices;
using ArenaApplication.Services;
using ArenaDomain.Entities.Bookings;
using ArenaDomain.Entities.User;
using ArenaDomain.Interfaces;
using ArenaInfrastructure;
using ArenaInfrastructure.Data;
using ArenaInfrastructure.Data.DataSeeding;
using ArenaInfrastructure.Repositories;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

namespace ArenaAPI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();

            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
            builder.Services.AddScoped<IMemberProfileRepository, MemberProfileRepository>();
            builder.Services.AddScoped<INotificationHub, NotificationHubService>();

            builder.Services.ConfigureDbContext(builder.Configuration);

            // From HEAD
            builder.Services.AddRepositories();
            builder.Services.AddApplicationServices();

            // Validators
            builder.Services.AddValidators();

            // SignalR
            builder.Services.AddSignalR();
            builder.Services.Configure<EmailSettings>(
              builder.Configuration.GetSection("EmailSettings"));


            builder.Services.AddHangfire(config =>
            {
                config.UseSqlServerStorage(
                    builder.Configuration.GetConnectionString("DefaultConnection"));
            });
            builder.Services.AddHangfireServer();

            // Background job service
            builder.Services.AddScoped<IBackgroundJobService, BackgroundJobService>();

            // OpenAPI + Bearer Auth
            builder.Services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            });

            // Identity
            builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();
            // JWT
            builder.Services.AddJwtAuthentication(builder.Configuration);

            // Auth Services
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped<IAuthRepository, AuthRepository>();
            builder.Services.AddScoped<IAuthService, AuthService>();

            // Profile Services
            builder.Services.AddScoped<IProfileService, ProfileService>();


            builder.Services.AddMemoryCache();
            builder.Services.AddScoped<IOtpService, OtpService>();
            // Booking Services
            builder.Services.AddScoped<IGenericRepository<Booking, Guid>,
                GenericRepository<Booking, Guid>>();
            builder.Services.AddScoped<IBookingService, BookingService>();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            var app = builder.Build();
            var emailSettings = app.Services.GetRequiredService<IOptions<EmailSettings>>().Value;
            Console.WriteLine($"SmtpServer: '{emailSettings.SmtpServer}'");
            Console.WriteLine($"Port: '{emailSettings.Port}'");
            Console.WriteLine($"Username: '{emailSettings.Username}'");
            // Seed database with initial data
            if (app.Environment.IsDevelopment())
            {
                using (var scope = app.Services.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    dbContext.Database.EnsureCreated();
                }
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference();
                app.MapGet("/", () => Results.Redirect("/scalar"));
                // Hangfire Dashboard (development only)
                app.UseHangfireDashboard();

            }

            app.UseCors("AllowAll");

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

                await DataSeeder.SeedAsync(context, userManager, roleManager);
            }

            app.MapControllers();
            app.MapHub<NotificationHub>("/hubs/notifications");

            
            app.Run();
        }
    }
}