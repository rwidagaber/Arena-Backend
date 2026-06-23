using ArenaApi.Configurations;
using ArenaApi.Configurations.BrearerConfig;
using ArenaApi.Configurations.JWTConfig;
using ArenaApi.Configurations.MapsterConfig;
using ArenaApi.Configurations.ValidatorConfig;
using ArenaApi.Hubs;
using ArenaApplication;
using ArenaApplication.IServices;
using ArenaApplication.IServices.IProgressServices;
using ArenaApplication.IServices.Payment;
using ArenaApplication.IServices.User;
using ArenaApplication.Services;

using ArenaApplication.Services.Payment;

using ArenaDomain.Entities;
using ArenaDomain.Entities.Bookings;
using ArenaDomain.Entities.User;
using ArenaDomain.Interfaces;
using ArenaDomain.Shared;
using ArenaInfrastructure;
using ArenaInfrastructure.AI;
using ArenaInfrastructure.Data;
using ArenaInfrastructure.Data.DataSeeding;
using ArenaInfrastructure.Localization;
using ArenaInfrastructure.Repositories;
using ArenaInfrastructure.Services;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
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
                options.DefaultRequestCulture = new RequestCulture("en-US");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
                options.RequestCultureProviders = new List<IRequestCultureProvider>
                {
                    new AcceptLanguageHeaderRequestCultureProvider(),
                    new CookieRequestCultureProvider(),
                    new QueryStringRequestCultureProvider()
                };
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
            builder.Services.AddSingleton<IAnalyticsCacheVersionService, AnalyticsCacheVersionService>();

            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
            builder.Services.AddScoped<IMemberProfileRepository, MemberProfileRepository>();
            builder.Services.AddScoped<INotificationHub, NotificationHubService>();
            builder.Services.AddScoped<IDashboardService, DashboardService>();
            builder.Services.AddScoped<IPushNotificationService, PushNotificationService>();

            builder.Services.Configure<EmailSettings>(
                builder.Configuration.GetSection("EmailSettings"));

            // ── Database ──────────────────────────────────────────────────
            // Registers: AppDbContext (SQL Server) + NpgsqlDataSource + NeonVectorStore (Neon)
            builder.Services.ConfigureDbContext(builder.Configuration);
            builder.Services.AddRepositories();
            builder.Services.AddApplicationServices();

            // ── Hangfire ──────────────────────────────────────────────────
            builder.Services.AddHangfire(config =>
                config.UseSqlServerStorage(
                    builder.Configuration.GetConnectionString("DefaultConnection")));
            builder.Services.AddHangfireServer();
            builder.Services.AddScoped<IBackgroundJobService, BackgroundJobService>();
            builder.Services.AddScoped<IBackgroundJobClient, BackgroundJobClient>();

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
            builder.Services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();

            // ── Profile ───────────────────────────────────────────────────
            builder.Services.AddScoped<IProfileService, ProfileService>();

            // ── Booking ───────────────────────────────────────────────────
            builder.Services.AddScoped<IGenericRepository<Booking, Guid>,
                GenericRepository<Booking, Guid>>();
            builder.Services.AddScoped<IBookingService, BookingService>();

            // ── Progress ───────────────────────────────────────────────────
            builder.Services.AddScoped<IProgressRepository, ProgressRepository>();
            builder.Services.AddScoped<IProgressService, ProgressService>();

            // ── Payment ───────────────────────────────────────────────────
            builder.Services.AddScoped<IUserQueryService, ArenaInfrastructure.Services.UserQueryService>();
            builder.Services.AddScoped<IPaymentService, PaymentService>();
            builder.Services.AddHttpClient<IPaymentGatewayService, ArenaInfrastructure.Services.PaymobService>();

            // ── AI / Chatbot Features ─────────────────────────────────────
            builder.Services.AddScoped<IChatService, ChatService>();
            builder.Services.AddScoped<IWorkoutAIService, WorkoutAIService>();
            builder.Services.AddScoped<INutritionAIService, NutritionAIService>();
            builder.Services.AddScoped<IBookingAIService, BookingAIService>();
            builder.Services.AddScoped<IGenericRepository<MemberProfile, Guid>, GenericRepository<MemberProfile, Guid>>();
            builder.Services.AddScoped<IRAGService, SimpleRAGService>();
            builder.Services.AddScoped<IMemberHealthRAGService, MemberHealthRAGService>();
            builder.Services.Configure<GeminiSettings>(
                builder.Configuration.GetSection("GeminiSettings"));

            builder.Services.AddHttpClient<IGeminiCompletionService, GeminiService>();
            builder.Services.AddHttpClient<IEmbeddingService, GeminiEmbeddingService>();

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

            // ── Seed Database + Init Vector Schema ────────────────────────
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

                if (app.Environment.IsDevelopment())
                    await context.Database.MigrateAsync();

                await DataSeeder.SeedAsync(context, userManager, roleManager);

                // ── Init pgvector schema on Neon (idempotent) ────────────
                // Creates the MemberHealthVectors table + HNSW index if they don't exist.
                var vectorStore = scope.ServiceProvider.GetService<NeonVectorStore>();
                if (vectorStore != null)
                {
                    try { await vectorStore.EnsureSchemaAsync(); }
                    catch (Exception ex) { Console.WriteLine($"[VectorStore] Schema init failed: {ex.Message}"); }
                }
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

            var localizationOptions = new RequestLocalizationOptions()
                .SetDefaultCulture("en-US")
                .AddSupportedCultures("en-US", "ar-EG")
                .AddSupportedUICultures("en-US", "ar-EG");

            app.UseRequestLocalization(localizationOptions);

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.MapHub<NotificationHub>("/hubs/notifications")
                .RequireAuthorization();

            app.Run();
        }
    }
}