using ArenaApi.Configurations.BrearerConfig;
using ArenaApi.Configurations.JWTConfig;
using ArenaApi.Configurations.MapsterConfig;
using ArenaApi.Configurations.ValidatorConfig;
using ArenaApplication.IServices;
using ArenaApplication.Services;
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


            //Add Mapster
            builder.Services.AddMapster();

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            });

            builder.Services.ConfigureDbContext(builder.Configuration);

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


            builder.Services.AddValidators();


            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped<IAuthRepository, AuthRepository>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IProfileService, ProfileService>();




            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference();

                app.MapGet("/", () => Results.Redirect("/scalar"));

            }


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

            app.Run();
        }
    }
}
