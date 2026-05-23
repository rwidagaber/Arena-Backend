using ArenaDomain.Entities.Chat;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaInfrastructure.Data.Configurations;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.MessageText)
               .IsRequired()
               .HasMaxLength(4000);

        builder.Property(m => m.AudioUrl)
               .IsRequired(false)
               .HasMaxLength(500);

        builder.Property(m => m.Sender)
               .IsRequired()
               .HasConversion<string>()
               .HasMaxLength(20);

        builder.Property(m => m.Intent)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(m => m.SentAt)
               .IsRequired();

        // Relationship configured from ChatConversationConfiguration

        builder.ToTable("ChatMessages");
    }
}
