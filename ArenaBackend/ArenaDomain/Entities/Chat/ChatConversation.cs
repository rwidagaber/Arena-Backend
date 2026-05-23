using ArenaDomain.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaDomain.Entities.Chat
{
    public class ChatConversation : BaseEntity<Guid>
    {
        public Guid MemberProfileId { get; set; }

        public virtual MemberProfile MemberProfile { get; set; } = null!;

        public string Title { get; set; } = string.Empty;

        public DateTime StartedAt { get; set; }

        // Navigation
        public virtual ICollection<ChatMessage> Messages { get; set; } = [];
    }
}
