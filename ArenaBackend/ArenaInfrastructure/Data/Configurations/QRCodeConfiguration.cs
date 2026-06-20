using ArenaDomain.Entities.Bookings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaInfrastructure.Data.Configurations;

public class QRCodeConfiguration : IEntityTypeConfiguration<QRCode>
{
    public void Configure(EntityTypeBuilder<QRCode> builder)
    {
        builder.HasKey(q => q.Id);

        builder.Property(q => q.Code)
               .IsRequired()
               .HasMaxLength(512);

        builder.Property(q => q.GeneratedAt)
               .IsRequired();

        builder.Property(q => q.ExpirationTime)
               .IsRequired();

        builder.Property(q => q.IsUsed)
               .IsRequired()
               .HasDefaultValue(false);

        // Unique index — one QR per booking
        builder.HasIndex(q => q.BookingId)
               .IsUnique();

        // Relationship configured from BookingConfiguration (principal side)

        builder.ToTable("QRCodes");
    }
}
