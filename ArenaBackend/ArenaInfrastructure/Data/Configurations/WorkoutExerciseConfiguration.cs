using ArenaDomain.Entities.Workout;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaInfrastructure.Data.Configurations;

public class WorkoutExerciseConfiguration : IEntityTypeConfiguration<WorkoutExercise>
{
    public void Configure(EntityTypeBuilder<WorkoutExercise> builder)
    {
        builder.HasKey(we => we.Id);

        builder.Property(we => we.Sets)
               .IsRequired();

        builder.Property(we => we.Reps)
               .IsRequired();

        builder.Property(we => we.Weight)
               .IsRequired(false)
               .HasColumnType("decimal(6,2)");

        builder.Property(we => we.DurationMinutes)
               .IsRequired(false);

        builder.Property(we => we.RestSeconds)
               .IsRequired(false);

        builder.Property(we => we.Notes)
               .IsRequired(false)
               .HasMaxLength(500);

        // Relationships configured from WorkoutDayConfiguration and ExerciseConfiguration

        builder.ToTable("WorkoutExercises");
    }
}
