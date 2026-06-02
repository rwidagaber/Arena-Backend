using ArenaApplication;
using ArenaInfrastructure;
using ArenaApplication.IServices;
using ArenaApplication.Services;
using ArenaInfrastructure.Repositories;
using ArenaDomain.Entities.Bookings;
using ArenaDomain.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.ConfigureDbContext(builder.Configuration);
builder.Services.AddApplicationServices();

// Booking dependencies (MVC Admin pages)
// Repositories and Unit of Work
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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
