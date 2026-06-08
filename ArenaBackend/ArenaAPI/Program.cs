using ArenaApi.Configurations;
using ArenaApi.Configurations.BrearerConfig;
using ArenaApi.Configurations.JWTConfig;
using ArenaApi.Configurations.MapsterConfig;
using ArenaApi.Configurations.ValidatorConfig;
using ArenaApi.Hubs;
using ArenaApplication;
using ArenaApplication.IServices;
using ArenaApplication.IServices.Payment;
using ArenaApplication.IServices.User;
using ArenaApplication.Services;
using ArenaApplication.Services.Payment;
using ArenaDomain.Entities.Bookings;
using ArenaDomain.Entities.User;
using ArenaDomain.Interfaces;
using ArenaDomain.Shared;
using ArenaInfrastructure;
using ArenaInfrastructure.Data;
using ArenaInfrastructure.Data.DataSeeding;
using ArenaInfrastructure.Localization;
using ArenaInfrastructure.Repositories;
using ArenaInfrastructure.Services;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Localization;
using Scalar.AspNetCore;
using System.Globalization;

namespace ArenaAPI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddApplicationServices();
            //Add Localization

            builder.Services.AddLocalization();

            builder.Services.AddSingleton<IStringLocalizerFactory, JsonStringLocalizerFactory>();
            // Mapster
            builder.Services.AddMapster();

            // Controllers
            builder.Services.AddControllers()
                // Localization for Data Annotations
                .AddDataAnnotationsLocalization(options =>
                {
                options.DataAnnotationLocalizerProvider = (type, factory) =>
                    factory.Create(typeof(ArenaLocalization));
                });


            // Localization options
            builder.Services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedCultures = new[]
                {
                    new CultureInfo("en-US"),
                    new CultureInfo("ar-EG")
                };

                options.DefaultRequestCulture = new RequestCulture("en-US");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;

                // Keep Accept-Language header as the primary provider
                options.RequestCultureProviders = new List<IRequestCultureProvider>
                {
                    new AcceptLanguageHeaderRequestCultureProvider(),
                    new CookieRequestCultureProvider(),
                    new QueryStringRequestCultureProvider()
                };
            });

            // Services
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
            builder.Services.AddScoped<IMemberProfileRepository, MemberProfileRepository>();
            builder.Services.AddScoped<INotificationHub, NotificationHubService>();
            builder.Services.AddScoped<IBackgroundJobService, BackgroundJobService>();
            builder.Services.AddScoped<IOtpService, OtpService>();
            builder.Services.AddMemoryCache();
            builder.Services.ConfigureDbContext(builder.Configuration);
            builder.Services.AddRepositories();
            builder.Services.AddApplicationServices();
            builder.Services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();
            builder.Services.AddMemoryCache();
            builder.Services.Configure<EmailSettings>(
                builder.Configuration.GetSection("EmailSettings"));
            // Validators
            builder.Services.AddValidators();

            // SignalR
            builder.Services.AddSignalR();

            // OpenAPI + Bearer
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

            // Auth
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped<IAuthRepository, AuthRepository>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddHangfire(config =>
            config.UseSqlServerStorage(
            builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddHangfireServer();
            builder.Services.AddScoped<IBackgroundJobClient, BackgroundJobClient>();

            // Profile
            builder.Services.AddScoped<IProfileService, ProfileService>();

            // Booking
            builder.Services.AddScoped<IGenericRepository<Booking, Guid>,
                GenericRepository<Booking, Guid>>();
            builder.Services.AddScoped<IBookingService, BookingService>();

            // Payment
            builder.Services.AddScoped<IUserQueryService, ArenaInfrastructure.Services.UserQueryService>();
            builder.Services.AddScoped<IPaymentService, PaymentService>();
            builder.Services.AddHttpClient<IPaymentGatewayService, ArenaInfrastructure.Services.PaymobService>();

            // Authorization
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("GymMemberOrAdmin", policy =>
                    policy.RequireRole("GymMember", "Admin"));
            });



            //QR Services
            builder.Services.AddScoped<IQRCodeService, QRCodeService>();
            builder.Services.AddScoped<IAttendanceService, AttendanceService>();
            

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

            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
                await DataSeeder.SeedAsync(context, userManager, roleManager);
            }

            // Middleware
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference();
                app.MapGet("/", () => Results.Redirect("/scalar"));
                app.UseHangfireDashboard();
            }

            app.UseCors("AllowAll");


            if (!app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            // Localization Middleware

            app.UseRequestLocalization();

            // Authentication & Authorization
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.MapHub<NotificationHub>("/hubs/notifications");

          


            app.Run();
        }
    }
}