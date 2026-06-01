using ArenaApi.ValidatorConfig;
using ArenaApplication.IServices.Payment;
using ArenaApplication.IServices.User;
using ArenaApplication.Mappers;
using ArenaApplication.Services.Payment;
using ArenaDomain.Entities.User;         
using ArenaDomain.Interfaces;
using ArenaInfrastructure;
using ArenaInfrastructure.Data;           
using ArenaInfrastructure.Repositories;
using ArenaInfrastructure.Services;
using Mapster;
using Microsoft.AspNetCore.Identity;

namespace ArenaAPI
{
    public class Program
    {
        public static async Task Main(string[] args) 
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddOpenApi();


            builder.Services.ConfigureDbContext(builder.Configuration);

            builder.Services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IPaymentService, PaymentService>();
            builder.Services.AddScoped<IUserQueryService, UserQueryService>();


            builder.Services.AddHttpClient<IPaymentGatewayService, PaymobService>();

            var mapsterConfig = TypeAdapterConfig.GlobalSettings;
            mapsterConfig.Scan(typeof(PaymentMappingConfig).Assembly);

            builder.Services.AddValidators();

            var app = builder.Build();

            await DataSeeder.SeedAsync(app.Services);  

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            if (!app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }
            app.UseAuthorization();
            app.MapControllers();

            app.Run();  
        }
    }
}