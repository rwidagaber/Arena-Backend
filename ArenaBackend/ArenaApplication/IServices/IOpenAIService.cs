using ArenaApplication.Dtos.ChatDtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.IServices
{
    public interface IOpenAIService
    {
        Task<string> GetCompletionAsync(
            string systemPrompt,
            List<ChatMessageDto> history,
            string userMessage
        );
    }
}
