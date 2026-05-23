using ArenaDomain.Entities.Bookings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaInfrastructure.Data.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.HasKey(b => b.Id);

  

        builder.Property(b => b.BookingDate)
               .IsRequired();

        builder.Property(b => b.StartTime)
               .IsRequired();

        builder.Property(b => b.EndTime)
               .IsRequired(false);

        builder.Property(b => b.Status)
               .IsRequired()
               .HasConversion<string>()
               .HasMaxLength(50);

        // MemberProfile → Bookings (many)
        builder.HasOne(b => b.MemberProfile)
               .WithMany(m => m.Bookings)
               .HasForeignKey(b => b.MemberProfileId)
               .OnDelete(DeleteBehavior.Restrict);

        // Booking → QRCode (one-to-one)
        builder.HasOne(b => b.QRCode)
               .WithOne(q => q.Booking)
               .HasForeignKey<QRCode>(q => q.BookingId)
               .OnDelete(DeleteBehavior.Cascade);

        // Booking → Attendance (one-to-one)
        builder.HasOne(b => b.Attendance)
               .WithOne(a => a.Booking)
               .HasForeignKey<Attendance>(a => a.BookingId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("Bookings");
    }
}
