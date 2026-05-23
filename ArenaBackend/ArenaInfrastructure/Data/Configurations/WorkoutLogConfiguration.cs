using ArenaDomain.Entities.Workout;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaInfrastructure.Data.Configurations;

public class WorkoutLogConfiguration : IEntityTypeConfiguration<WorkoutLog>
{
    public void Configure(EntityTypeBuilder<WorkoutLog> builder)
    {
        builder.HasKey(w => w.Id);

        builder.Property(w => w.WorkoutDate)
               .IsRequired();

        builder.Property(w => w.Notes)
               .IsRequired(false)
               .HasMaxLength(1000);

        // MemberProfile → WorkoutLogs (many)
        builder.HasOne(w => w.MemberProfile)
               .WithMany()
               .HasForeignKey(w => w.MemberProfileId)
               .OnDelete(DeleteBehavior.Restrict);

        // WorkoutPlan → WorkoutLogs (many, optional)
        builder.HasOne(w => w.WorkoutPlan)
               .WithMany()
               .HasForeignKey(w => w.WorkoutPlanId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.SetNull);

        builder.ToTable("WorkoutLogs");
    }
}
