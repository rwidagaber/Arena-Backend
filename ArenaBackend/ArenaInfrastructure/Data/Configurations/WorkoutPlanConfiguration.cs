using ArenaDomain.Entities.Workout;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaInfrastructure.Data.Configurations;

public class WorkoutPlanConfiguration : IEntityTypeConfiguration<WorkoutPlan>
{
    public void Configure(EntityTypeBuilder<WorkoutPlan> builder)
    {
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Name)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(w => w.DurationWeeks)
               .IsRequired();

        builder.Property(w => w.IsActive)
               .IsRequired()
               .HasDefaultValue(true);

        builder.Property(w => w.AssignedTrainerId)
               .IsRequired(false);

        // MemberProfile → WorkoutPlans (many)
        builder.HasOne(w => w.MemberProfile)
               .WithMany(m => m.WorkoutPlans)
               .HasForeignKey(w => w.MemberProfileId)
               .OnDelete(DeleteBehavior.Restrict);

        // WorkoutPlan → WorkoutDays (many)
        builder.HasMany(w => w.WorkoutDays)
               .WithOne(d => d.WorkoutPlan)
               .HasForeignKey(d => d.WorkoutPlanId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("WorkoutPlans");
    }
}
