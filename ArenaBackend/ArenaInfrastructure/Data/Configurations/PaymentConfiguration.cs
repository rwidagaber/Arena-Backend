using ArenaDomain.Entities.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaInfrastructure.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
       public void Configure(EntityTypeBuilder<Payment> builder)
       {
              builder.HasKey(p => p.Id);

              builder.Property(p => p.Amount)
                     .IsRequired()
                     .HasColumnType("decimal(10,2)");

              builder.Property(p => p.Currency)
                     .IsRequired()
                     .HasMaxLength(10)
                     .HasDefaultValue("EGP");

              builder.Property(p => p.PaymentMethod)
                     .IsRequired()
                     .HasConversion<string>()
                     .HasMaxLength(50);

              builder.Property(p => p.TransactionId)
                     .IsRequired(false)
                     .HasMaxLength(200);

              builder.Property(p => p.PaymentIntentId)
                     .IsRequired(false)
                     .HasMaxLength(200);

              builder.Property(p => p.Status)
                     .IsRequired()
                     .HasConversion<string>()
                     .HasMaxLength(50);

              builder.Property(p => p.PaymentDate)
                     .IsRequired(false);

              builder.Property(p => p.FailureReason)
                     .IsRequired(false)
                     .HasMaxLength(500);

              builder.Property(p => p.GatewayResponse)
                     .IsRequired(false)
                     .HasColumnType("nvarchar(max)");

              // ApplicationUser → Payments (many)
              builder.HasOne(p => p.User)
                     .WithMany(u => u.Payments)
                     .HasForeignKey(p => p.UserId)
                     .OnDelete(DeleteBehavior.Restrict);

              // UserSubscription → Payments (many) — optional
              builder.HasOne(p => p.UserSubscription)
                     .WithMany(us => us.Payments)
                     .HasForeignKey(p => p.UserSubscriptionId)
                     .IsRequired(false)
                     .OnDelete(DeleteBehavior.SetNull);

              builder.HasIndex(p => p.TransactionId)
                     .IsUnique()
                     .HasFilter("[TransactionId] IS NOT NULL");

              builder.HasIndex(p => new { p.Status, p.PaymentDate });
              builder.HasIndex(p => p.PaymentDate);

              builder.ToTable("Payments");
       }
}
