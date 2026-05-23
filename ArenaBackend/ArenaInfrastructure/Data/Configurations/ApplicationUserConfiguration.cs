using ArenaDomain.Entities;
using ArenaDomain.Entities.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaInfrastructure.Data.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.FirstName)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(u => u.LastName)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(u => u.PreferredLanguage)
               .IsRequired()
               .HasMaxLength(10)
               .HasDefaultValue("en");

        builder.Property(u => u.IsActive)
               .IsRequired()
               .HasDefaultValue(true);

        // ApplicationUser → MemberProfile (one-to-one)
        builder.HasOne(u => u.MemberProfile)
               .WithOne(mp => mp.User)
               .HasForeignKey<MemberProfile>(mp => mp.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        // Notifications and Payments relationships configured from their own configurations
    }
}
