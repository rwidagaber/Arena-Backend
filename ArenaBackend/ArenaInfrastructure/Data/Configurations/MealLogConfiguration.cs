using ArenaDomain.Entities.Nutrition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaInfrastructure.Data.Configurations;

public class MealLogConfiguration : IEntityTypeConfiguration<MealLog>
{
    public void Configure(EntityTypeBuilder<MealLog> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.ImageUrl)
               .IsRequired()
               .HasMaxLength(500);

        builder.Property(m => m.Calories)
               .IsRequired()
               .HasColumnType("decimal(8,2)");

        builder.Property(m => m.Protein)
               .IsRequired()
               .HasColumnType("decimal(6,2)");

        builder.Property(m => m.Carbs)
               .IsRequired()
               .HasColumnType("decimal(6,2)");

        builder.Property(m => m.Fat)
               .IsRequired()
               .HasColumnType("decimal(6,2)");

        builder.Property(m => m.FoodItems)
               .IsRequired()
               .HasMaxLength(2000);

        builder.Property(m => m.AIComment)
               .IsRequired(false)
               .HasMaxLength(1000);

        builder.Property(m => m.LogDate)
               .IsRequired();

        // MemberProfile → MealLogs (many)
        builder.HasOne(m => m.MemberProfile)
               .WithMany(mp => mp.MealLogs)
               .HasForeignKey(m => m.MemberProfileId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("MealLogs");
    }
}
