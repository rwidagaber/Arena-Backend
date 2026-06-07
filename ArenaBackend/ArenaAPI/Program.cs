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
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using System.Globalization;

namespace ArenaAPI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ── Localization ──────────────────────────────────────────────
            builder.Services.AddLocalization();
            builder.Services.AddSingleton<IStringLocalizerFactory, JsonStringLocalizerFactory>();

            var supportedCultures = new[]
            {
                new CultureInfo("en-US"),
                new CultureInfo("ar-EG")
            };

            builder.Services.Configure<RequestLocalizationOptions>(options =>
            {
                options.DefaultRequestCulture = new RequestCulture(
                    culture: supportedCultures[0],
                    uiCulture: supportedCultures[0]);
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
                options.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider());
            });

            // ── Controllers ───────────────────────────────────────────────
            builder.Services.AddControllers()
                .AddDataAnnotationsLocalization(options =>
                {
                    options.DataAnnotationLocalizerProvider = (type, factory) =>
                        factory.Create(typeof(ArenaLocalization));
                });

            // ── Core Services ─────────────────────────────────────────────
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddMemoryCache();

            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
            builder.Services.AddScoped<IMemberProfileRepository, MemberProfileRepository>();
            builder.Services.AddScoped<INotificationHub, NotificationHubService>();

            builder.Services.Configure<EmailSettings>(
                builder.Configuration.GetSection("EmailSettings"));

            // ── Database ──────────────────────────────────────────────────
            builder.Services.ConfigureDbContext(builder.Configuration);
            builder.Services.AddRepositories();
            builder.Services.AddApplicationServices();

            // ── Hangfire ──────────────────────────────────────────────────
            builder.Services.AddHangfire(config =>
                config.UseSqlServerStorage(
                    builder.Configuration.GetConnectionString("DefaultConnection")));
            builder.Services.AddHangfireServer();
            builder.Services.AddScoped<IBackgroundJobService, BackgroundJobService>();

            // ── Mapster ───────────────────────────────────────────────────
            builder.Services.AddMapster();

            // ── Validators ────────────────────────────────────────────────
            builder.Services.AddValidators();

            // ── SignalR ───────────────────────────────────────────────────
            builder.Services.AddSignalR();

            // ── OpenAPI + Bearer Auth ─────────────────────────────────────
            builder.Services.AddOpenApi(options =>
                options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

            // ── Identity ──────────────────────────────────────────────────
            builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

            // ── JWT ───────────────────────────────────────────────────────
            builder.Services.AddJwtAuthentication(builder.Configuration);

            // ── Auth ──────────────────────────────────────────────────────
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped<IAuthRepository, AuthRepository>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IOtpService, OtpService>();

            // ── Profile ───────────────────────────────────────────────────
            builder.Services.AddScoped<IProfileService, ProfileService>();

            // ── Booking ───────────────────────────────────────────────────
            builder.Services.AddScoped<IGenericRepository<Booking, Guid>,
                GenericRepository<Booking, Guid>>();
            builder.Services.AddScoped<IBookingService, BookingService>();

            // ── Payment ───────────────────────────────────────────────────
            builder.Services.AddScoped<IUserQueryService, UserQueryService>();
            builder.Services.AddScoped<IPaymentService, PaymentService>();
            builder.Services.AddHttpClient<IPaymentGatewayService, PaymobService>();

            // ── Authorization Policies ────────────────────────────────────
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("GymMemberOrAdmin", policy =>
                    policy.RequireRole("GymMember", "Admin"));
            });

            // ── QR / Attendance ───────────────────────────────────────────
            builder.Services.AddScoped<IQRCodeService, QRCodeService>();
            builder.Services.AddScoped<IAttendanceService, AttendanceService>();

            // ── CORS ──────────────────────────────────────────────────────
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader());
            });

            // ═════════════════════════════════════════════════════════════
            var app = builder.Build();
            // ═════════════════════════════════════════════════════════════

            // ── Log email settings (dev helper) ───────────────────────────
            var emailSettings = app.Services.GetRequiredService<IOptions<EmailSettings>>().Value;
//             Console.WriteLine($"SmtpServer: '{emailSettings.SmtpServer}'");
//             Console.WriteLine($"Port: '{emailSettings.Port}'");
//             Console.WriteLine($"Username: '{emailSettings.Username}'");

            // ── Seed Database ─────────────────────────────────────────────
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

                if (app.Environment.IsDevelopment())
                    context.Database.EnsureCreated();

                await DataSeeder.SeedAsync(context, userManager, roleManager);
            }

            // ── Middleware Pipeline ───────────────────────────────────────
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference();
                app.MapGet("/", () => Results.Redirect("/scalar"));
                app.UseHangfireDashboard();
            }

            app.UseCors("AllowAll");

            if (!app.Environment.IsDevelopment())
                app.UseHttpsRedirection();

            // Localization middleware
            var localizationOptions = new RequestLocalizationOptions()
                .SetDefaultCulture("en-US")
                .AddSupportedCultures("en-US", "ar-EG")
                .AddSupportedUICultures("en-US", "ar-EG");

            app.UseRequestLocalization(localizationOptions);

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.MapHub<NotificationHub>("/hubs/notifications");

            app.Run();
        }
    }
}