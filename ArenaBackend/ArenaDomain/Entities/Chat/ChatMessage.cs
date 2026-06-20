using ArenaDomain.Enums;
using ArenaDomain.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaDomain.Entities.Chat
{
    public class ChatMessage : BaseEntity<Guid>
    {
        public Guid ChatConversationId { get; set; }

        public virtual ChatConversation ChatConversation { get; set; } = null!;

        public string MessageText { get; set; } = string.Empty;

        public string? AudioUrl { get; set; }

        public SenderType Sender { get; set; }

        public string Intent { get; set; } = string.Empty;

        public DateTime SentAt { get; set; }
    }
}
