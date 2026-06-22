using ArenaDomain.Entities.Gym;
using ArenaDomain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;

namespace ArenaInfrastructure.Data.Configurations
{
    public class WorkingHoursConfiguration : IEntityTypeConfiguration<WorkingHours>
    {
        public void Configure(EntityTypeBuilder<WorkingHours> builder)
        {
            builder.HasKey(wh => wh.Id);

            builder.Property(wh => wh.DayOfWeek)
                   .IsRequired()
                   .HasConversion<string>()
                   .HasMaxLength(20);

            builder.Property(wh => wh.OpenTime)
                   .IsRequired();

            builder.Property(wh => wh.CloseTime)
                   .IsRequired();

            builder.Property(wh => wh.IsClosed)
                   .IsRequired()
                   .HasDefaultValue(false);

            builder.ToTable("WorkingHours");

            SeedWorkingHours(builder);
        }

        private static void SeedWorkingHours(EntityTypeBuilder<WorkingHours> builder)
        {
            var seedDate = new DateTime(2026, 6, 20, 0, 0, 0, DateTimeKind.Utc);
            var workingHoursList = new List<WorkingHours>
            {
                new WorkingHours
                {
                    Id = 1,
                    DayOfWeek = WorkingDay.Monday,
                    OpenTime = new TimeSpan(8, 0, 0),
                    CloseTime = new TimeSpan(3, 0, 0),
                    IsClosed = false,
                    CreatedAt = seedDate,
                    IsDeleted = false
                },
                new WorkingHours
                {
                    Id = 2,
                    DayOfWeek = WorkingDay.Tuesday,
                    OpenTime = new TimeSpan(8, 0, 0),
                    CloseTime = new TimeSpan(3, 0, 0),
                    IsClosed = false,
                    CreatedAt = seedDate,
                    IsDeleted = false
                },
                new WorkingHours
                {
                    Id = 3,
                    DayOfWeek = WorkingDay.Wednesday,
                    OpenTime = new TimeSpan(8, 0, 0),
                    CloseTime = new TimeSpan(3, 0, 0),
                    IsClosed = false,
                    CreatedAt = seedDate,
                    IsDeleted = false
                },
                new WorkingHours
                {
                    Id = 4,
                    DayOfWeek = WorkingDay.Thursday,
                    OpenTime = new TimeSpan(8, 0, 0),
                    CloseTime = new TimeSpan(3, 0, 0),
                    IsClosed = false,
                    CreatedAt = seedDate,
                    IsDeleted = false
                },
                new WorkingHours
                {
                    Id = 5,
                    DayOfWeek = WorkingDay.Friday,
                    OpenTime = new TimeSpan(15, 0, 0),
                    CloseTime = new TimeSpan(3, 0, 0),
                    IsClosed = false,
                    CreatedAt = seedDate,
                    IsDeleted = false
                },
                new WorkingHours
                {
                    Id = 6,
                    DayOfWeek = WorkingDay.Saturday,
                    OpenTime = new TimeSpan(8, 0, 0),
                    CloseTime = new TimeSpan(3, 0, 0),
                    IsClosed = false,
                    CreatedAt = seedDate,
                    IsDeleted = false
                },
                new WorkingHours
                {
                    Id = 7,
                    DayOfWeek = WorkingDay.Sunday,
                    OpenTime = new TimeSpan(8, 0, 0),
                    CloseTime = new TimeSpan(3, 0, 0),
                    IsClosed = false,
                    CreatedAt = seedDate,
                    IsDeleted = false
                }
            };

            builder.HasData(workingHoursList);
        }
    }
}
