using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Dtos.ChatDtos
{
    public class ChatMessageDto
    {
        public string Role { get; set; } = string.Empty; // "user" or "assistant"
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
