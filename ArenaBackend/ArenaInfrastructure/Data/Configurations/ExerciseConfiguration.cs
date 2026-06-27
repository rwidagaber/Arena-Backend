using ArenaDomain.Entities.Workout;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaInfrastructure.Data.Configurations;

public class ExerciseConfiguration : IEntityTypeConfiguration<Exercise>
{
    public void Configure(EntityTypeBuilder<Exercise> builder)
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

        builder.Property(e => e.Equipment)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(e => e.EquipmentAr)
               .IsRequired(false)
               .HasMaxLength(100);

        builder.Property(e => e.VideoUrl)
               .IsRequired(false)
               .HasMaxLength(500);

        builder.Property(e => e.ImageUrl)
               .IsRequired(false)
               .HasMaxLength(500);

        // MemberProfile → Exercises (many)
        builder.HasOne(e => e.MemberProfile)
               .WithMany()
               .HasForeignKey(e => e.MemberProfileId)
               .OnDelete(DeleteBehavior.Cascade);

        // Exercise → WorkoutExercises (many)
        builder.HasMany(e => e.WorkoutExercises)
               .WithOne(we => we.Exercise)
               .HasForeignKey(we => we.ExerciseId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable("Exercises");
    }
}
