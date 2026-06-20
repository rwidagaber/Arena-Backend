using ArenaDomain.Entities.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaInfrastructure.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Title)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(n => n.Message)
               .IsRequired()
               .HasMaxLength(1000);

        builder.Property(n => n.Type)
               .IsRequired()
               .HasConversion<string>()
               .HasMaxLength(50);

        builder.Property(n => n.IsRead)
               .IsRequired()
               .HasDefaultValue(false);

        // MemberProfile → Notifications (many)
        builder.HasOne(n => n.MemberProfile)
       .WithMany(mp => mp.Notifications)
       .HasForeignKey(n => n.MemberProfileId)
       .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("Notifications");
    }
}
