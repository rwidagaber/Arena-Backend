using ArenaDomain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaInfrastructure.Data.Configurations;

public class MemberHealthVectorConfiguration : IEntityTypeConfiguration<MemberHealthVector>
{
    public void Configure(EntityTypeBuilder<MemberHealthVector> builder)
    {
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Content)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(v => v.Category)
            .IsRequired()
            .HasMaxLength(100);

        // Nullable legacy column — kept for backward compatibility
        builder.Property(v => v.EmbeddingJson)
            .IsRequired(false);

        builder.Property(v => v.RecordedAt)
            .IsRequired();

        builder.Property(v => v.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(v => v.MemberProfileId);

        builder.HasOne(v => v.MemberProfile)
            .WithMany(p => p.HealthVectors)
            .HasForeignKey(v => v.MemberProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("MemberHealthVectors");
    }
}
