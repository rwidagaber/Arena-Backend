using ArenaApplication.Dtos.ChatDtos;
using ArenaApplication.IServices;
using ArenaInfrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ArenaInfrastructure.AI
{
    /// <summary>
    /// RAG service for member health data.
    /// - Embeddings: Gemini text-embedding-004 (768 dims)
    /// - Vector store: Neon PostgreSQL + pgvector via NeonVectorStore (raw Npgsql)
    /// - App data: SQL Server via AppDbContext (unchanged)
    /// </summary>
    public class MemberHealthRAGService : IMemberHealthRAGService
    {
        private readonly IEmbeddingService _embeddingService;
        private readonly IGeminiCompletionService _gemini;
        private readonly AppDbContext _context;       // SQL Server — profiles, plans, etc.
        private readonly NeonVectorStore _vectorStore; // Neon PostgreSQL — embeddings

        private static readonly string[] CriticalCategories =
        [
            "Injury", "Disease", "HealthCondition", "Allergy",
            "Medication", "DietaryRestriction", "PhysicalLimitation", "ProfileSync"
        ];

        public MemberHealthRAGService(
            IEmbeddingService embeddingService,
            IGeminiCompletionService gemini,
            AppDbContext context,
            NeonVectorStore vectorStore)
        {
            _embeddingService = embeddingService;
            _gemini = gemini;
            _context = context;
            _vectorStore = vectorStore;
        }

        // ── Save ──────────────────────────────────────────────────────────────────

        public async Task SaveHealthInfoAsync(Guid memberProfileId, string content, string category)
        {
            if (string.IsNullOrWhiteSpace(content))
                return;

            if (await _vectorStore.ExistsAsync(memberProfileId, content))
                return;

            var embedding = await _embeddingService.GetEmbeddingAsync(content);
            if (embedding.Length == 0)
                Console.WriteLine($"[RAG] ⚠️ Saving without embedding (Gemini call failed): {content}");

            await _vectorStore.InsertAsync(memberProfileId, content,
                string.IsNullOrWhiteSpace(category) ? "ChatMention" : category,
                embedding.Length > 0 ? embedding : null);

            Console.WriteLine($"[RAG] ✅ Saved health vector [{category}] for member {memberProfileId}");
        }

        // ── Sync profile data ────────────────────────────────────────────────────

        public async Task SyncProfileHealthDataAsync(Guid memberProfileId)
        {
            var profile = await _context.MemberProfiles
                .FirstOrDefaultAsync(p => p.Id == memberProfileId || p.UserId == memberProfileId);

            if (profile == null)
                return;

            // Soft-delete stale ProfileSync rows before re-inserting fresh ones
            await _vectorStore.SoftDeleteByCategoryAsync(profile.Id, "ProfileSync");

            if (!string.IsNullOrWhiteSpace(profile.Injuries))
                await SaveHealthInfoAsync(profile.Id, $"Member has the following injury: {profile.Injuries}", "ProfileSync");

            if (!string.IsNullOrWhiteSpace(profile.HealthConditions))
                await SaveHealthInfoAsync(profile.Id, $"Member has the following health condition: {profile.HealthConditions}", "ProfileSync");

            if (!string.IsNullOrWhiteSpace(profile.DietaryRestrictions))
                await SaveHealthInfoAsync(profile.Id, $"Member has the following dietary restriction: {profile.DietaryRestrictions}", "ProfileSync");
        }

        // ── Extract health facts from chat ───────────────────────────────────────

        public async Task ExtractAndSaveFromChatAsync(Guid memberProfileId, string userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
                return;

            var extractPrompt = $$"""
You are a health information extractor for a gym app.
Analyze this user message and determine if it mentions any injury, pain, disease,
diagnosed health condition, illness, allergy, medication, dietary restriction,
pregnancy, surgery, or physical limitation that should be remembered for future AI plans.

Message: "{{userMessage}}"

Return ONLY valid JSON, no markdown:
{
  "hasHealthInfo": true or false,
  "items": [
    {
      "extractedInfo": "clear English sentence describing one health fact",
      "category": "Injury" or "Disease" or "HealthCondition" or "Allergy" or "Medication" or "DietaryRestriction" or "PhysicalLimitation"
    }
  ]
}

Examples:
"I have knee pain and diabetes"
=> {"hasHealthInfo":true,"items":[{"extractedInfo":"Member has knee pain","category":"Injury"},{"extractedInfo":"Member has diabetes","category":"Disease"}]}

"عندي ألم في الركبة من امبارح"
=> {"hasHealthInfo":true,"items":[{"extractedInfo":"Member reported knee pain starting recently","category":"Injury"}]}

"what should I eat before training"
=> {"hasHealthInfo":false,"items":[]}

Only flag actual member health facts, not general questions.
""";

            try
            {
                var response = await _gemini.GetCompletionAsync(
                    extractPrompt,
                    new List<ChatMessageDto>(),
                    userMessage);

                var clean = CleanJson(response);
                var result = JsonSerializer.Deserialize<HealthExtractResult>(
                    clean,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result?.HasHealthInfo != true)
                    return;

                var items = result.Items.Count > 0
                    ? result.Items
                    : string.IsNullOrWhiteSpace(result.ExtractedInfo)
                        ? []
                        :
                        [
                            new HealthExtractItem
                            {
                                ExtractedInfo = result.ExtractedInfo,
                                Category = result.Category
                            }
                        ];

                foreach (var item in items.Where(i => !string.IsNullOrWhiteSpace(i.ExtractedInfo)))
                    await SaveHealthInfoAsync(memberProfileId, item.ExtractedInfo, item.Category);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RAG] Health extraction failed: {ex.Message}");
            }
        }

        // ── Retrieve context via pgvector ANN search ──────────────────────────────

        public async Task<string> GetRelevantHealthContextAsync(Guid memberProfileId, string query, int topK = 5)
        {
            // Always include critical health categories (injuries, diseases, etc.)
            var criticalContent = await _vectorStore.GetCriticalContentAsync(memberProfileId, CriticalCategories);

            // Get query embedding for semantic search
            var queryEmbedding = await _embeddingService.GetEmbeddingAsync(query);

            if (queryEmbedding.Length == 0)
            {
                // Fallback: return critical items only if embedding failed
                return criticalContent.Count > 0
                    ? string.Join("\n", criticalContent.Select(c => $"- {c}"))
                    : string.Empty;
            }

            // pgvector ANN search — uses HNSW index inside PostgreSQL (<=> cosine distance)
            var semanticHits = await _vectorStore.SearchBySimilarityAsync(memberProfileId, queryEmbedding, topK);

            // cosine distance < 0.55  ≈  cosine similarity > 0.45
            var relevant = semanticHits
                .Where(r => r.Distance < 0.55)
                .Select(r => r.Content);

            var combined = criticalContent.Union(relevant).Distinct().ToList();

            Console.WriteLine($"[RAG] 🔍 pgvector search: {semanticHits.Count} candidates → {combined.Count} returned for query '{query}'");

            return combined.Count > 0
                ? string.Join("\n", combined.Select(c => $"- {c}"))
                : string.Empty;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static string CleanJson(string raw)
        {
            var clean = raw.Trim();
            if (clean.StartsWith("```json")) clean = clean[7..];
            else if (clean.StartsWith("```")) clean = clean[3..];
            if (clean.EndsWith("```")) clean = clean[..^3];

            var start = clean.IndexOf('{');
            var end = clean.LastIndexOf('}');
            if (start >= 0 && end > start)
                clean = clean.Substring(start, end - start + 1);

            return clean.Trim();
        }
    }

    public class HealthExtractResult
    {
        public bool HasHealthInfo { get; set; }
        public List<HealthExtractItem> Items { get; set; } = [];
        public string ExtractedInfo { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    public class HealthExtractItem
    {
        public string ExtractedInfo { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }
}
