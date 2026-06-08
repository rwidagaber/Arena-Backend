using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Dtos.ChatDtos
{
    public class ChatResponseWithHistoryDto
    {
        public Guid ConversationId { get; set; }
        public string Reply { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
