using ArenaDomain.Entities.Workout;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaInfrastructure.Data.Configurations;

public class ExerciseCatalogItemConfiguration : IEntityTypeConfiguration<ExerciseCatalogItem>
{
    public void Configure(EntityTypeBuilder<ExerciseCatalogItem> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
               .IsRequired()
               .HasMaxLength(150);

        builder.Property(e => e.NameAr)
               .IsRequired(false)
               .HasMaxLength(150);

        builder.Property(e => e.Description)
               .IsRequired()
               .HasMaxLength(1000);

        builder.Property(e => e.DescriptionAr)
               .IsRequired(false)
               .HasMaxLength(1000);

        builder.Property(e => e.MuscleGroup)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(e => e.MuscleGroupAr)
               .IsRequired(false)
               .HasMaxLength(100);

        builder.ToTable("ExerciseCatalogItems");
    }
}
