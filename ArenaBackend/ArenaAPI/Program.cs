using ArenaApi.ValidatorConfig;
using ArenaApplication.IServices;
using ArenaApplication.Services;
using ArenaDomain.Entities.Bookings;
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

            builder.Services.AddControllers();

            builder.Services.AddEndpointsApiExplorer();

                  builder.Services.AddSwaggerGen();

            builder.Services.AddValidators();

            builder.Services.ConfigureDbContext(builder.Configuration);

            builder.Services.AddScoped<IGenericRepository<Booking, Guid>, GenericRepository<Booking, Guid>>();

            builder.Services.AddScoped<IBookingService, BookingService>();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();

                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();


            //any thing for pr

        }
    }
}