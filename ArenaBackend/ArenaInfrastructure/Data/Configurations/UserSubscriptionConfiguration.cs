using ArenaDomain.Entities.Subscription;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaInfrastructure.Data.Configurations;

public class UserSubscriptionConfiguration : IEntityTypeConfiguration<UserSubscription>
{
       public void Configure(EntityTypeBuilder<UserSubscription> builder)
       {
              builder.HasKey(us => us.Id);

              builder.Property(us => us.StartDate)
                     .IsRequired();

              builder.Property(us => us.EndDate)
                     .IsRequired();

              builder.Property(us => us.Status)
                     .IsRequired()
                     .HasConversion<string>()
                     .HasMaxLength(50);

              builder.Property(us => us.RemainingSessions)
                     .IsRequired();

              builder.Property(us => us.ReminderSent)
                     .IsRequired()
                     .HasDefaultValue(false);

              // MemberProfile → UserSubscriptions (many)
              builder.HasOne(us => us.MemberProfile)
                     .WithMany(m => m.Subscriptions)
                     .HasForeignKey(us => us.MemberProfileId)
                     .OnDelete(DeleteBehavior.Restrict);

              // Relationship to SubscriptionPlan configured from SubscriptionPlanConfiguration
              // Relationship to Payments configured from PaymentConfiguration

              builder.HasIndex(us => new { us.Status, us.EndDate });
              builder.HasIndex(us => us.CreatedAt);

              builder.ToTable("UserSubscriptions");
       }
}
