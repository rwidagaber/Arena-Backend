using ArenaApplication;
using ArenaApplication.IServices;
using ArenaApplication.IServices.User;
using ArenaApplication.Services;
using ArenaDomain.Entities.Bookings;
using ArenaDomain.Entities.Subscription;
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
using ArenaMVC.Services;
using Hangfire;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLocalization();

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IStringLocalizerFactory, DbStringLocalizerFactory>();

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { new CultureInfo("en-US"), new CultureInfo("ar-EG") };
    options.DefaultRequestCulture = new RequestCulture(supportedCultures[0], supportedCultures[0]);
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider());
});

builder
    .Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options =>
    {
        options.DataAnnotationLocalizerProvider = (type, factory) =>
            factory.Create(typeof(ArenaLocalization));
    });

builder.Services.ConfigureDbContext(builder.Configuration);

// Register EmailSettings configuration section for DI options pattern
builder.Services.Configure<ArenaApi.Configurations.EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

// register Mapster IMapper and config
builder.Services.AddMapsterConfiguration();
builder.Services.AddApplicationServices();

// ── ASP.NET Identity (for UserManager / role checks only — no sign-in manager) ──
// AddIdentityCore does NOT register its own cookie scheme,
// so our /Auth/Login path is preserved.
builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
.AddRoles<IdentityRole<Guid>>()
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// ── Cookie Authentication for MVC Admin Portal ────────────────────
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath        = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan   = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly  = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.Name      = "ArenaAdminAuth";
    });

// ── Admin Login Service (lightweight — no JWT) ────────────────────
builder.Services.AddScoped<IMvcAdminLoginService, MvcAdminLoginService>();

// User-related services
builder.Services.AddScoped<IUserQueryService, UserQueryService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ArenaMVC.Services.IDashboardDataSeeder, ArenaMVC.Services.DashboardDataSeederService>();
builder.Services.AddSingleton<IAnalyticsCacheVersionService, AnalyticsCacheVersionService>();

// Booking dependencies (MVC Admin pages)
builder.Services.AddScoped<IGenericRepository<Booking, Guid>, GenericRepository<Booking, Guid>>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IBackgroundJobService, BackgroundJobService>();

// Notification-related services (minimal set required by BookingService)
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IMemberProfileRepository, MemberProfileRepository>();

// Provide no-op implementations for the MVC app (admin UI doesn't need realtime/push notifications)
builder.Services.AddScoped<INotificationHub, ArenaMVC.Services.NoopNotificationHub>();
builder.Services.AddScoped<IPushNotificationService, ArenaMVC.Services.NoopPushNotificationService>();
// ── Hangfire ──────────────────────────────────────────────────
builder.Services.AddHangfire(config =>
    config.UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHangfireServer();
builder.Services.AddScoped<IBackgroundJobService, BackgroundJobService>();
builder.Services.AddScoped<IBackgroundJobClient, BackgroundJobClient>();

// Booking service
builder.Services.AddScoped<IBookingService, BookingService>();

// QR Check-In dependencies
builder.Services.AddScoped<IQRCodeService, QRCodeService>();
builder.Services.AddScoped<IGenericRepository<QRCode, Guid>, GenericRepository<QRCode, Guid>>();
builder.Services.AddScoped<IGenericRepository<Attendance, Guid>, GenericRepository<Attendance, Guid>>();
builder.Services.AddScoped<IGenericRepository<UserSubscription, Guid>, GenericRepository<UserSubscription, Guid>>();

builder.Services.AddScoped<IAttendanceSuggestionService, AttendanceSuggestionService>();

// 2. FIX: Add the missing dependency right here!
//builder.Services.AddScoped<IGeminiCompletionService, GeminiService>();
builder.Services.AddHttpClient<IGeminiCompletionService, GeminiService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context     = scope.ServiceProvider.GetRequiredService<ArenaInfrastructure.Data.AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

    await context.Database.MigrateAsync();
    await TranslationSeeder.SeedAsync(context);

    // Seed roles + admin user (required for cookie-based admin login)
    await DataSeeder.SeedAsync(context, userManager, roleManager);

    // ── Hangfire Recurring No-Show Penalty Job ────────────────────
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    recurringJobManager.AddOrUpdate<INoShowPenaltyService>(
        "NoShowPenaltyJob",
        service => service.ProcessNoShowPenaltiesAsync(CancellationToken.None),
        Cron.Minutely());

    // Dashboard demo data is now seeded on-demand via Admin Dashboard > Generate Demo Data
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRequestLocalization(
    app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value
);

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllers();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
