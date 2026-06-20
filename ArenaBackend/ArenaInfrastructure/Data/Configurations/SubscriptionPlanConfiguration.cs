using ArenaDomain.Entities.Subscription;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaInfrastructure.Data.Configurations;

public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.NameEn)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(s => s.NameAr)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(s => s.DurationMonths)
               .IsRequired();

        builder.Property(s => s.Price)
               .IsRequired()
               .HasColumnType("decimal(10,2)");

        builder.Property(s => s.SessionLimit)
               .IsRequired(false);

        builder.Property(s => s.IsActive)
               .IsRequired()
               .HasDefaultValue(true);

        // SubscriptionPlan → UserSubscriptions (many)
        builder.HasMany(s => s.UserSubscriptions)
               .WithOne(us => us.Plan)
               .HasForeignKey(us => us.PlanId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable("SubscriptionPlans");

        // Seed initial subscription plans
        SeedSubscriptionPlans(builder);
    }

    private static void SeedSubscriptionPlans(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        var seedDate = new DateTime(2026, 5, 31, 18, 14, 57, 893, DateTimeKind.Utc);
        var plans = new List<SubscriptionPlan>
        {
            new SubscriptionPlan
            {
                Id = new Guid("11111111-1111-1111-1111-111111111111"),
                NameEn = "Basic",
                NameAr = "أساسي",
                DescriptionEn = "Perfect for beginners to get started with fitness",
                DescriptionAr = "مثالي للمبتدئين للبدء في اللياقة البدنية",
                DurationMonths = 1,
                Price = 9.99m,
                SessionLimit = 4,
                IsActive = true,
                CreatedAt = seedDate,
                IsDeleted = false
            },
            new SubscriptionPlan
            {
                Id = new Guid("22222222-2222-2222-2222-222222222222"),
                NameEn = "Premium",
                NameAr = "بريميوم",
                DescriptionEn = "Full access to all facilities and premium classes",
                DescriptionAr = "الوصول الكامل إلى جميع المرافق والفئات المتميزة",
                DurationMonths = 3,
                Price = 24.99m,
                SessionLimit = 12,
                IsActive = true,
                CreatedAt = seedDate,
                IsDeleted = false
            },
            new SubscriptionPlan
            {
                Id = new Guid("33333333-3333-3333-3333-333333333333"),
                NameEn = "Elite",
                NameAr = "نخبة",
                DescriptionEn = "Unlimited access with personal trainer sessions",
                DescriptionAr = "وصول غير محدود مع جلسات المدرب الشخصي",
                DurationMonths = 12,
                Price = 79.99m,
                SessionLimit = null,
                IsActive = true,
                CreatedAt = seedDate,
                IsDeleted = false
            }
        };

        builder.HasData(plans);
    }
}
