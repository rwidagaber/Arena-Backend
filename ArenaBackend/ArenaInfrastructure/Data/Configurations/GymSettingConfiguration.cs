using ArenaDomain.Entities.Gym;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace ArenaInfrastructure.Data.Configurations
{
    public class GymSettingConfiguration : IEntityTypeConfiguration<GymSetting>
    {
        public void Configure(EntityTypeBuilder<GymSetting> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.NoShowThreshold)
                   .IsRequired()
                   .HasDefaultValue(2);

            builder.Property(x => x.IsNoShowPenaltyEnabled)
                   .IsRequired()
                   .HasDefaultValue(true);

            builder.ToTable("GymSettings");

            builder.HasData(new GymSetting
            {
                Id = 1,
                NoShowThreshold = 2,
                IsNoShowPenaltyEnabled = true,
                CreatedAt = new DateTime(2026, 6, 24, 0, 0, 0, DateTimeKind.Utc),
                IsDeleted = false
            });
        }
    }
}
