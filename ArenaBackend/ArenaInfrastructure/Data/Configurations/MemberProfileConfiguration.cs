using ArenaDomain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaInfrastructure.Data.Configurations;

public class MemberProfileConfiguration : IEntityTypeConfiguration<MemberProfile>
{
    public void Configure(EntityTypeBuilder<MemberProfile> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.DateOfBirth)
               .IsRequired();

        builder.Property(m => m.Weight)
               .IsRequired(false)
               .HasColumnType("decimal(6,2)");

        builder.Property(m => m.Height)
               .IsRequired(false)
               .HasColumnType("decimal(5,2)");

        builder.Property(m => m.BMI)
               .IsRequired(false)
               .HasColumnType("decimal(5,2)");

        builder.Property(m => m.Gender)
               .IsRequired()
               .HasConversion<string>()
               .HasMaxLength(20);

        builder.Property(m => m.ProfileImageUrl)
               .IsRequired(false)
               ;

        // Unique: one profile per user
        builder.HasIndex(m => m.UserId)
               .IsUnique();

        // All child relationships are configured from the child configurations
        // (Bookings, Attendances, WorkoutPlans, NutritionPlans,
        //  MealLogs, ProgressLogs, ChatConversations, UserSubscriptions)

        builder.ToTable("MemberProfiles");
    }
}
