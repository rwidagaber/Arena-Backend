using ArenaDomain.Entities.Workout;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaInfrastructure.Data.Configurations;

public class WorkoutDayConfiguration : IEntityTypeConfiguration<WorkoutDay>
{
    public void Configure(EntityTypeBuilder<WorkoutDay> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.DayNumber)
               .IsRequired();

        builder.Property(d => d.DayName)
               .IsRequired()
               .HasMaxLength(50);

        // Relationship configured from WorkoutPlanConfiguration

        // WorkoutDay → WorkoutExercises (many)
        builder.HasMany(d => d.Exercises)
               .WithOne(e => e.WorkoutDay)
               .HasForeignKey(e => e.WorkoutDayId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("WorkoutDays");
    }
}
