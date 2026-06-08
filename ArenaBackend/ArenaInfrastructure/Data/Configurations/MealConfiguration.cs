using ArenaDomain.Entities.Nutrition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaInfrastructure.Data.Configurations;

public class MealConfiguration : IEntityTypeConfiguration<Meal>
{
    public void Configure(EntityTypeBuilder<Meal> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.MealType)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(m => m.Name)
               .IsRequired()
               .HasMaxLength(200);

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

        builder.Property(m => m.Ingredients)
               .IsRequired()
               .HasMaxLength(2000);

        builder.HasOne(m => m.NutritionPlan)
               .WithMany(n => n.Meals)
               .HasForeignKey(m => m.NutritionPlanId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("Meals");
    }
}
