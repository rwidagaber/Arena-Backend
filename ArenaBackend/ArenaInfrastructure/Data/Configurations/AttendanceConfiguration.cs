using ArenaDomain.Entities.Bookings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaInfrastructure.Data.Configurations;

public class AttendanceConfiguration : IEntityTypeConfiguration<Attendance>
{
       public void Configure(EntityTypeBuilder<Attendance> builder)
       {
              builder.HasKey(a => a.Id);

              builder.Property(a => a.CheckInTime)
                     .IsRequired(false);

              builder.Property(a => a.ScannedById)
                     .IsRequired(false);

              // Relationship configured from BookingConfiguration
              // MemberProfile → Attendances (many)
              builder.HasOne(a => a.MemberProfile)
                     .WithMany(m => m.Attendances)
                     .HasForeignKey(a => a.MemberProfileId)
                     .OnDelete(DeleteBehavior.Restrict);

              builder.HasIndex(a => a.CheckInTime);
              builder.HasIndex(a => new { a.MemberProfileId, a.CheckInTime });

              builder.ToTable("Attendances");
       }
}
