// ArenaInfrastructure/Data/DataSeeder.cs
using ArenaDomain.Entities.Subscription;
using ArenaInfrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (!db.SubscriptionPlans.Any())
        {
            db.SubscriptionPlans.AddRange(
                new SubscriptionPlan
                {
                    Id = Guid.NewGuid(),
                    NameEn = "Monthly Basic",
                    NameAr = "شهري أساسي",
                    DescriptionEn = "Basic monthly membership",
                    DescriptionAr = "عضوية شهرية أساسية",
                    DurationMonths = 1,
                    Price = 200,
                    SessionLimit = 20,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new SubscriptionPlan
                {
                    Id = Guid.NewGuid(),
                    NameEn = "Quarterly Pro",
                    NameAr = "ربع سنوي برو",
                    DescriptionEn = "3-month membership",
                    DescriptionAr = "عضوية 3 أشهر",
                    DurationMonths = 3,
                    Price = 500,
                    SessionLimit = null,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            );

            await db.SaveChangesAsync();
        }
    }
}