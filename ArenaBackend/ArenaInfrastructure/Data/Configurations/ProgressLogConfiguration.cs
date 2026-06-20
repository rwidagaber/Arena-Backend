using ArenaDomain.Entities.Health;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaInfrastructure.Data.Configurations;

public class ProgressLogConfiguration : IEntityTypeConfiguration<ProgressLog>
{
    public void Configure(EntityTypeBuilder<ProgressLog> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Weight)
               .IsRequired()
               .HasColumnType("decimal(6,2)");

        builder.Property(p => p.BodyFat)
               .IsRequired(false)
               .HasColumnType("decimal(5,2)");

        builder.Property(p => p.MuscleMass)
               .IsRequired(false)
               .HasColumnType("decimal(6,2)");

        builder.Property(p => p.LoggedAt)
               .IsRequired();

        // MemberProfile → ProgressLogs (many)
        builder.HasOne(p => p.MemberProfile)
               .WithMany(m => m.ProgressLogs)
               .HasForeignKey(p => p.MemberProfileId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("ProgressLogs");
    }
}
