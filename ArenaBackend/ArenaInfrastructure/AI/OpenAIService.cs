using ArenaApplication.Dtos.ChatDtos;
using ArenaApplication.IServices;
using ArenaApplication.Settings;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace ArenaApplication.Services.AI
{
    public class OpenAIService : IOpenAIService
    {
        private readonly OpenAISettings _settings;
        private readonly HttpClient _client;

        public OpenAIService(HttpClient client, IOptions<OpenAISettings> settings)
        {
            _client = client;
            _settings = settings.Value;
        }

        public async Task<string> GetCompletionAsync(
      string systemPrompt,
      List<ChatMessageDto> history,
      string userMessage)
        {
            Console.WriteLine($"=== API KEY: {_settings.ApiKey} ===");

            // ✅ Groq URL
            var url = "https://api.groq.com/openai/v1/chat/completions";

            var messages = new List<object>
    {
        new { role = "system", content = systemPrompt }
    };

            foreach (var msg in history)
                messages.Add(new { role = msg.Role, content = msg.Content });

            messages.Add(new { role = "user", content = userMessage });

            var body = new
            {
                model = _settings.Model,
                messages
            };

            // ✅ Add Groq auth header
            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Add(
                "Authorization", $"Bearer {_settings.ApiKey}");

            var response = await _client.PostAsJsonAsync(url, body);

            var rawJson = await response.Content.ReadAsStringAsync();
            Console.WriteLine("=== GROQ RAW RESPONSE ===");
            Console.WriteLine(rawJson);
            Console.WriteLine("=========================");

            var json = JsonSerializer.Deserialize<JsonElement>(rawJson);

            if (json.TryGetProperty("error", out var error))
            {
                var errorMessage = error.GetProperty("message").GetString();
                throw new Exception($"Groq Error: {errorMessage}");
            }

            // ✅ Same structure as OpenAI
            return json
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;
        }
    }
}