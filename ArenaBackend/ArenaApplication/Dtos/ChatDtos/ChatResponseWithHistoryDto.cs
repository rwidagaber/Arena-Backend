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
        public string Intent { get; set; } = "chat";
        public string? Action { get; set; }
        public bool BookingChanged { get; set; }
    }
}
