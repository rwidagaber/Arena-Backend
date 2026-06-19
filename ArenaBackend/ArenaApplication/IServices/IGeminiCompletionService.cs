using ArenaApplication.Dtos.ChatDtos;

namespace ArenaApplication.IServices
{
    public interface IGeminiCompletionService
    {
        Task<string> GetCompletionAsync(
            string systemPrompt,
            List<ChatMessageDto> history,
            string userMessage);

        Task<string> GetVisionCompletionAsync(
            string systemPrompt,
            string userMessage,
            string imageMimeType,
            string imageBase64);
    }
}
