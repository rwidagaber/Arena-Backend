using ArenaDomain.Entities.Workout;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaInfrastructure.Data.Configurations;

public class MuscleGroupConfiguration : IEntityTypeConfiguration<MuscleGroup>
{
    public void Configure(EntityTypeBuilder<MuscleGroup> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
               .IsRequired()
               .HasMaxLength(150);

        builder.Property(c => c.NameAr)
               .IsRequired()
               .HasMaxLength(150);

        builder.ToTable("MuscleGroups");
    }
}
