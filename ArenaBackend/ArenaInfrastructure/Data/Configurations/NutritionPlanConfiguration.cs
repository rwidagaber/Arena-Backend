using ArenaDomain.Entities.Nutrition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaInfrastructure.Data.Configurations;

public class NutritionPlanConfiguration : IEntityTypeConfiguration<NutritionPlan>
{
    public void Configure(EntityTypeBuilder<NutritionPlan> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.StartDate)
               .IsRequired();

        builder.Property(n => n.DailyCalories)
               .IsRequired()
               .HasColumnType("decimal(8,2)");

        builder.Property(n => n.ProteinGrams)
               .IsRequired()
               .HasColumnType("decimal(6,2)");

        builder.Property(n => n.CarbsGrams)
               .IsRequired()
               .HasColumnType("decimal(6,2)");

        builder.Property(n => n.FatGrams)
               .IsRequired()
               .HasColumnType("decimal(6,2)");

        builder.Property(n => n.IsActive)
               .IsRequired()
               .HasDefaultValue(true);

        // MemberProfile → NutritionPlans (many)
        builder.HasOne(n => n.MemberProfile)
               .WithMany(m => m.NutritionPlans)
               .HasForeignKey(n => n.MemberProfileId)
               .OnDelete(DeleteBehavior.Cascade);

        // NutritionPlan → Meals (many)
        builder.HasMany(n => n.Meals)
               .WithOne(m => m.NutritionPlan)
               .HasForeignKey(m => m.NutritionPlanId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("NutritionPlans");
    }
}
