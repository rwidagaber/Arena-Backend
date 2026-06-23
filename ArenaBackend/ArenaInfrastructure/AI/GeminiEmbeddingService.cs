using ArenaApplication.IServices;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace ArenaInfrastructure.AI
{
    public class GeminiEmbeddingService : IEmbeddingService
    {
        private readonly HttpClient _client;
        private readonly GeminiSettings _settings;

        public GeminiEmbeddingService(HttpClient client, IOptions<GeminiSettings> settings)
        {
            _client = client;
            _settings = settings.Value;
        }

        public async Task<float[]> GetEmbeddingAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return [];

            var url = $"{_settings.BaseUrl}/text-embedding-004:embedContent?key={_settings.ApiKey}";
            var body = new
            {
                model = "models/gemini-embedding-001",
                content = new
                {
                    parts = new[] { new { text } }
                }
            };

            try
            {
                var response = await _client.PostAsJsonAsync(url, body);
                var responseText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Gemini embedding error: {responseText}");
                    return [];
                }

                var json = JsonSerializer.Deserialize<JsonElement>(responseText);
                return json
                    .GetProperty("embedding")
                    .GetProperty("values")
                    .EnumerateArray()
                    .Select(e => e.GetSingle())
                    .ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Gemini embedding failed: {ex.Message}");
                return [];
            }
        }
    }
}
