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
    }
}
