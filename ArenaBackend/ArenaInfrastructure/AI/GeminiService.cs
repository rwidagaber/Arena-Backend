using ArenaApplication.Dtos.ChatDtos;
using ArenaApplication.IServices;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ArenaInfrastructure.AI
{
    public class GeminiService : IGeminiCompletionService
    {
        private readonly HttpClient _client;
        private readonly GeminiSettings _settings;

        public GeminiService(HttpClient client, IOptions<GeminiSettings> settings)
        {
            _client = client;
            _settings = settings.Value;
        }

        public async Task<string> GetCompletionAsync(
            string systemPrompt,
            List<ChatMessageDto> history,
            string userMessage)
        {
            var apiKey = GetConfiguredApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("Gemini API key is not configured. Set GeminiSettings:ApiKey or GEMINI_API_KEY.");

            var baseUrl = string.IsNullOrWhiteSpace(_settings.BaseUrl)
                ? "https://generativelanguage.googleapis.com/v1beta/models"
                : _settings.BaseUrl.TrimEnd('/');

            var body = BuildRequestBody(systemPrompt, history, userMessage);
            var models = GetModelsToTry().ToList();
            Exception? lastException = null;

            foreach (var model in models)
            {
                for (var attempt = 1; attempt <= 2; attempt++)
                {
                    try
                    {
                        return await SendCompletionRequestAsync(baseUrl, apiKey, model, body);
                    }
                    catch (Exception ex) when (IsRetryableGeminiError(ex))
                    {
                        lastException = ex;

                        if (attempt < 2)
                            await Task.Delay(TimeSpan.FromMilliseconds(700 * attempt));
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        throw;
                    }
                }
            }

            throw new Exception(
                "Gemini models are temporarily busy or rate limited. Tried: " +
                string.Join(", ", models) +
                $". Last error: {lastException?.Message}");
        }

        private static object BuildRequestBody(
            string systemPrompt,
            List<ChatMessageDto> history,
            string userMessage)
        {
            var contents = new List<object>();

            foreach (var msg in history.Where(m => !string.IsNullOrWhiteSpace(m.MessageText)))
            {
                contents.Add(new
                {
                    role = IsAssistantRole(msg.Sender) ? "model" : "user",
                    parts = new[] { new { text = msg.MessageText } }
                });
            }

            contents.Add(new
            {
                role = "user",
                parts = new[] { new { text = userMessage } }
            });

            return new
            {
                contents,
                systemInstruction = string.IsNullOrWhiteSpace(systemPrompt) ? null : new
                {
                    parts = new[] { new { text = systemPrompt } }
                },
                generationConfig = new
                {
                    temperature = 0.7,
                    maxOutputTokens = 2048
                }
            };
        }

        private async Task<string> SendCompletionRequestAsync(
            string baseUrl,
            string apiKey,
            string model,
            object body)
        {
            var url = $"{baseUrl}/{model}:generateContent?key={Uri.EscapeDataString(apiKey)}";
            var response = await _client.PostAsJsonAsync(url, body);
            var responseText = await response.Content.ReadAsStringAsync();

            JsonElement json;
            try
            {
                json = JsonSerializer.Deserialize<JsonElement>(responseText);
            }
            catch (JsonException ex)
            {
                throw new Exception("Gemini provider returned an invalid JSON response.", ex);
            }

            if (!response.IsSuccessStatusCode)
            {
                var message = TryReadProviderError(json)
                    ?? $"Gemini provider request failed with status {(int)response.StatusCode}.";

                throw new GeminiProviderException(response.StatusCode, model, message);
            }

            var text = TryReadCompletionText(json);
            if (string.IsNullOrWhiteSpace(text))
                throw new Exception($"Gemini provider response did not include valid completion text from {model}.");

            return text;
        }

        private string GetConfiguredApiKey()
        {
            if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
                return _settings.ApiKey.Trim();

            return Environment.GetEnvironmentVariable("GEMINI_API_KEY")?.Trim()
                ?? Environment.GetEnvironmentVariable("GOOGLE_API_KEY")?.Trim()
                ?? string.Empty;
        }

        private IEnumerable<string> GetModelsToTry()
        {
            var primary = string.IsNullOrWhiteSpace(_settings.Model)
                ? "gemini-2.5-flash-lite"
                : _settings.Model.Trim();

            return new[] { primary }
                .Concat(_settings.FallbackModels ?? [])
                .Where(model => !string.IsNullOrWhiteSpace(model))
                .Select(model => model.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private static bool IsRetryableGeminiError(Exception ex)
        {
            if (ex is not GeminiProviderException geminiEx)
                return false;

            return geminiEx.StatusCode == HttpStatusCode.TooManyRequests ||
                geminiEx.StatusCode == HttpStatusCode.ServiceUnavailable ||
                geminiEx.StatusCode == HttpStatusCode.InternalServerError ||
                geminiEx.Message.Contains("high demand", StringComparison.OrdinalIgnoreCase) ||
                geminiEx.Message.Contains("quota", StringComparison.OrdinalIgnoreCase) ||
                geminiEx.Message.Contains("rate", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsAssistantRole(string? sender)
        {
            return string.Equals(sender, "assistant", StringComparison.OrdinalIgnoreCase)
                || string.Equals(sender, "model", StringComparison.OrdinalIgnoreCase)
                || string.Equals(sender, "ai", StringComparison.OrdinalIgnoreCase);
        }

        private static string? TryReadCompletionText(JsonElement json)
        {
            if (!json.TryGetProperty("candidates", out var candidates) ||
                candidates.ValueKind != JsonValueKind.Array ||
                candidates.GetArrayLength() == 0 ||
                !candidates[0].TryGetProperty("content", out var content) ||
                !content.TryGetProperty("parts", out var parts) ||
                parts.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var textElement))
                    return textElement.GetString();
            }

            return null;
        }

        private static string? TryReadProviderError(JsonElement json)
        {
            if (!json.TryGetProperty("error", out var error))
                return null;

            if (error.ValueKind == JsonValueKind.Object &&
                error.TryGetProperty("message", out var message))
            {
                return message.GetString();
            }

            return error.ToString();
        }

        private sealed class GeminiProviderException : Exception
        {
            public GeminiProviderException(HttpStatusCode statusCode, string model, string message)
                : base($"{message} Model: {model}")
            {
                StatusCode = statusCode;
            }

            public HttpStatusCode StatusCode { get; }
        }
    }
}
