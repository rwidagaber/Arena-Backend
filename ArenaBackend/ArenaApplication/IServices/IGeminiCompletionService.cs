using ArenaApplication.Dtos.ChatDtos;

namespace ArenaApplication.IServices
{
    public interface IGeminiCompletionService
    {
        Task<string> GetCompletionAsync(
            string systemPrompt,
            List<ChatMessageDto> history,
            string userMessage);
    }
}
