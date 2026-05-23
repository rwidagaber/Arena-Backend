using ArenaDomain.Entities.Chat;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaInfrastructure.Data.Configurations;

public class ChatConversationConfiguration : IEntityTypeConfiguration<ChatConversation>
{
    public void Configure(EntityTypeBuilder<ChatConversation> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Title)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(c => c.StartedAt)
               .IsRequired();

        // MemberProfile → ChatConversations (many)
        builder.HasOne(c => c.MemberProfile)
               .WithMany(m => m.ChatConversations)
               .HasForeignKey(c => c.MemberProfileId)
               .OnDelete(DeleteBehavior.Cascade);

        // ChatConversation → Messages (many)
        builder.HasMany(c => c.Messages)
               .WithOne(m => m.ChatConversation)
               .HasForeignKey(m => m.ChatConversationId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("ChatConversations");
    }
}
