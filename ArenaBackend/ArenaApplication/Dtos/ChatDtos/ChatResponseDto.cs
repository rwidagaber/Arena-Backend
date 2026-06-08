using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Dtos.ChatDtos
{
    public class ChatResponseDto
    {
        public Guid Id { get; set; }
        public string MessageText { get; set; } = string.Empty;
        public string Sender { get; set; } = string.Empty; // "User" or "AI"
        public string Intent { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
    }
}
