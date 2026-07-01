using ArenaApplication.Dtos.ChatDtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.IServices
{
    public interface IChatService
    {
        Task<ChatResponseWithHistoryDto> SendMessageAsync(
        Guid memberProfileId,
        Guid? conversationId,
        string userMessage);
        Task<VoiceChatResponseDto> SendVoiceMessageAsync(
        Guid memberProfileId,
        Guid? conversationId,
        Stream audio,
        string audioContentType);
        Task<List<ConversationDto>> GetConversationsAsync(Guid memberProfileId);
        Task<List<ChatResponseDto>> GetConversationMessagesAsync(Guid conversationId);
        Task<ConversationDto> CreateConversationAsync(CreateConversationDto dto);
        Task DeleteConversationAsync(Guid conversationId);
    }
}
