using ArenaApi.Configurations.BrearerConfig;
using ArenaApi.Configurations.JWTConfig;
using ArenaApi.Configurations.ValidatorConfig;
using ArenaApi.Hubs;
using ArenaApplication.IServices;
using ArenaApplication.Services;
using ArenaDomain.Entities.Bookings;
using ArenaDomain.Entities.User;
using ArenaDomain.Interfacees;
using ArenaInfrastructure;
using ArenaInfrastructure.Data;
using ArenaInfrastructure.Data.DataSeeding;
using ArenaInfrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
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
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<INotificationHub, NotificationHubService>();
            builder.Services.ConfigureDbContext(builder.Configuration);
            builder.Services.AddValidators();
            builder.Services.AddSignalR();
            builder.Services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            });

            builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
            })
                       .AddEntityFrameworkStores<AppDbContext>()
                       .AddDefaultTokenProviders();

            builder.Services.AddJwtAuthentication(builder.Configuration);


            


            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped<IAuthRepository, AuthRepository>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IProfileService, ProfileService>();



            builder.Services.AddScoped<IGenericRepository<Booking, Guid>, GenericRepository<Booking, Guid>>();

            builder.Services.AddScoped<IBookingService, BookingService>();  

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference();

                app.MapGet("/", () => Results.Redirect("/scalar"));

            }

//             app.UseCors("AllowAll");

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
