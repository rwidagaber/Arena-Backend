using ArenaDomain.Entities.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaInfrastructure.Data.Configurations;

public class TranslationConfiguration : IEntityTypeConfiguration<Translation>
{
    public void Configure(EntityTypeBuilder<Translation> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Key)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(t => t.Value)
            .IsRequired();

        builder.Property(t => t.Language)
            .IsRequired()
            .HasMaxLength(10);

        builder.HasIndex(t => new { t.Key, t.Language }).IsUnique();

        builder.ToTable("Translations");
    }
}
