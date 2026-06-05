using ArenaApplication;
using ArenaInfrastructure;
using ArenaApplication.IServices;
using ArenaApplication.Services;
using ArenaInfrastructure.Repositories;
using ArenaInfrastructure.Localization;
using ArenaInfrastructure.Data.DataSeeding;
using ArenaDomain.Entities.Bookings;
using ArenaDomain.Interfaces;
using ArenaDomain.Shared;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Localization;
using System.Globalization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLocalization();

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IStringLocalizerFactory, DbStringLocalizerFactory>();

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[]
    {
        new CultureInfo("en-US"),
        new CultureInfo("ar-EG")
    };
    options.DefaultRequestCulture = new RequestCulture(supportedCultures[0], supportedCultures[0]);
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider());
});

builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options =>
    {
        options.DataAnnotationLocalizerProvider = (type, factory) =>
            factory.Create(typeof(ArenaLocalization));
    });

builder.Services.ConfigureDbContext(builder.Configuration);
builder.Services.AddApplicationServices();

// Booking dependencies (MVC Admin pages)
builder.Services.AddScoped<IGenericRepository<Booking, Guid>, GenericRepository<Booking, Guid>>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Notification-related services (minimal set required by BookingService)
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IMemberProfileRepository, MemberProfileRepository>();
// Provide a no-op NotificationHub implementation for the MVC app (admin UI doesn't need realtime pushes)
builder.Services.AddScoped<INotificationHub, ArenaMVC.Services.NoopNotificationHub>();

// Booking service
builder.Services.AddScoped<IBookingService, BookingService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ArenaInfrastructure.Data.AppDbContext>();
    await context.Database.MigrateAsync();
    await TranslationSeeder.SeedAsync(context);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

var supportedCultures = new[] { "en-US", "ar-EG" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
