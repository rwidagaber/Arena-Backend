using System;

namespace ArenaApplication.Dtos.ChatDtos
{
    public class VoiceChatResponseDto
    {
        // What Gemini heard — lets the UI show "You said: ..."
        public string Transcript { get; set; } = string.Empty;

        public Guid ConversationId { get; set; }
        public string Reply { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
