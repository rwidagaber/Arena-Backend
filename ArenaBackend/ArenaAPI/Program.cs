using ArenaApi.ValidatorConfig;
using ArenaApplication.IServices.Payment;
using ArenaApplication.Mappers;
using ArenaApplication.Services.Payment;
using ArenaDomain.Interfaces;
using ArenaInfrastructure;
using ArenaInfrastructure.Repositories;
using Mapster;
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


            builder.Services.ConfigureDbContext(builder.Configuration);

            builder.Services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IPaymentService, PaymentService>();

            builder.Services.AddHttpClient<ArenaInfrastructure.Services.PaymobService>();

            // Mapster
            var mapsterConfig = TypeAdapterConfig.GlobalSettings;
            mapsterConfig.Scan(typeof(PaymentMappingConfig).Assembly);


            builder.Services.AddValidators();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }


            
            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
