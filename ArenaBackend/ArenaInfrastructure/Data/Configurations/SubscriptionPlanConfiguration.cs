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

        builder.Property(s => s.HasAI)
               .IsRequired()
               .HasDefaultValue(false);

        builder.Property(s => s.DiscountPercentage)
               .IsRequired(false)
               .HasColumnType("decimal(5,2)");

        builder.Property(s => s.DiscountEndDate)
               .IsRequired(false);

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
            // --- Standard Plans (Without AI) ---
            new SubscriptionPlan
            {
                Id = new Guid("11111111-1111-1111-1111-111111111111"),
                NameEn = "Basic",
                NameAr = "أساسي",
                DescriptionEn = "Essential access to gym facilities and class bookings",
                DescriptionAr = "دخول أساسي لصالة الألعاب الرياضية وحجز الحصص",
                DurationMonths = 1,
                Price = 400.00m,
                SessionLimit = 24,
                IsActive = true,
                HasAI = false,
                CreatedAt = seedDate,
                IsDeleted = false
            },
            new SubscriptionPlan
            {
                Id = new Guid("22222222-2222-2222-2222-222222222222"),
                NameEn = "Pro",
                NameAr = "برو",
                DescriptionEn = "Extended standard access with savings",
                DescriptionAr = "دخول قياسي ممتد مع توفير في التكلفة",
                DurationMonths = 3,
                Price = 1000.00m,
                SessionLimit = 72,
                IsActive = true,
                HasAI = false,
                CreatedAt = seedDate,
                IsDeleted = false
            },
            new SubscriptionPlan
            {
                Id = new Guid("33333333-3333-3333-3333-333333333333"),
                NameEn = "Premium",
                NameAr = "بريميوم",
                DescriptionEn = "Mid-term gym access for consistent athletes",
                DescriptionAr = "دخول متوسط المدى للرياضيين المستمرين",
                DurationMonths = 6,
                Price = 1800.00m,
                SessionLimit = 144,
                IsActive = true,
                HasAI = false,
                CreatedAt = seedDate,
                IsDeleted = false
            },
            new SubscriptionPlan
            {
                Id = new Guid("44444444-4444-4444-4444-444444444444"),
                NameEn = "Max",
                NameAr = "ماكس",
                DescriptionEn = "Full year essential gym access",
                DescriptionAr = "دخول أساسي لصالة الألعاب الرياضية لمدة عام كامل",
                DurationMonths = 12,
                Price = 3000.00m,
                SessionLimit = 288,
                IsActive = true,
                HasAI = false,
                CreatedAt = seedDate,
                IsDeleted = false
            },

            // --- With AI Plans ---
            new SubscriptionPlan
            {
                Id = new Guid("55555555-5555-5555-5555-555555555555"),
                NameEn = "Basic AI",
                NameAr = "أساسي ذكي",
                DescriptionEn = "1 Month of full fitness access + AI Coach guidance",
                DescriptionAr = "اشتراك لمدة شهر مع توجيه كامل من مدرب الذكاء الاصطناعي",
                DurationMonths = 1,
                Price = 500.00m,
                SessionLimit = 24,
                IsActive = true,
                HasAI = true,
                CreatedAt = seedDate,
                IsDeleted = false
            },
            new SubscriptionPlan
            {
                Id = new Guid("66666666-6666-6666-6666-666666666666"),
                NameEn = "Pro AI",
                NameAr = "برو ذكي",
                DescriptionEn = "3 Months of full access + AI Coach (Most Popular)",
                DescriptionAr = "اشتراك لمدة 3 أشهر مع مدرب الذكاء الاصطناعي (الأكثر شعبية)",
                DurationMonths = 3,
                Price = 1100.00m,
                SessionLimit = 72,
                IsActive = true,
                HasAI = true,
                CreatedAt = seedDate,
                IsDeleted = false
            },
            new SubscriptionPlan
            {
                Id = new Guid("77777777-7777-7777-7777-777777777777"),
                NameEn = "Premium AI",
                NameAr = "بريميوم ذكي",
                DescriptionEn = "6 Months of full access + AI Coach guidance",
                DescriptionAr = "اشتراك لمدة 6 أشهر مع توجيه كامل من مدرب الذكاء الاصطناعي",
                DurationMonths = 6,
                Price = 1900.00m,
                SessionLimit = 144,
                IsActive = true,
                HasAI = true,
                CreatedAt = seedDate,
                IsDeleted = false
            },
            new SubscriptionPlan
            {
                Id = new Guid("88888888-8888-8888-8888-888888888888"),
                NameEn = "Max AI",
                NameAr = "ماكس ذكي",
                DescriptionEn = "1 Year of complete personalized fitness (Best Value)",
                DescriptionAr = "سنة كاملة من اللياقة المخصصة مع مدرب الذكاء الاصطناعي (أفضل قيمة)",
                DurationMonths = 12,
                Price = 3100.00m,
                SessionLimit = 288,
                IsActive = true,
                HasAI = true,
                CreatedAt = seedDate,
                IsDeleted = false
            }
        };

        builder.HasData(plans);
    }
}
