using ArenaApplication.IServices;
using ArenaInfrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace ArenaInfrastructure.AI
{
    public class SimpleRAGService : IRAGService
    {
        private static readonly object ChunkLock = new();
        private static List<KnowledgeChunk> _chunks = [];
        private readonly AppDbContext _context;

        public SimpleRAGService(AppDbContext context)
        {
            _context = context;
        }

        // ✅ Load knowledge on startup
        public Task IndexKnowledgeBaseAsync()
        {
            lock (ChunkLock)
            {
                _chunks = LoadKnowledgeChunks();
            }

            Console.WriteLine($"✅ RAG: Loaded {_chunks.Count} knowledge chunks");
            return Task.CompletedTask;
        }

        public Task IndexMemberDataAsync(Guid memberProfileId)
            => Task.CompletedTask;

        // ✅ Smart keyword search
        public Task<string> SearchAsync(string query, int topK = 5)
        {
            EnsureKnowledgeLoaded();

            if (!_chunks.Any())
                return Task.FromResult(string.Empty);

            var queryWords = NormalizeQuery(query).ToList();
            var queryText = NormalizeText(query);

            var scored = _chunks
                .Select(chunk => new
                {
                    chunk.Content,
                    chunk.Category,
                    Score = CalculateScore(chunk, queryWords, queryText)
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Take(topK)
                .ToList();

            if (!scored.Any())
                return Task.FromResult(string.Empty);

            var result = string.Join("\n\n---\n\n",
                scored.Select(s => s.Content));

            Console.WriteLine($"=== RAG Found {scored.Count} relevant chunks ===");
            return Task.FromResult(result);
        }

        // ✅ Search member-specific data from DB
        public async Task<string> SearchMemberDataAsync(
            Guid memberProfileId, string query)
        {
            var profile = await _context.MemberProfiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == memberProfileId);

            if (profile == null) return string.Empty;

            var parts = new List<string>();
            var name = string.IsNullOrWhiteSpace(profile.User?.FirstName)
                ? profile.FirstName ?? "Member"
                : profile.User.FirstName;

            parts.Add($"Member name from ApplicationUser: {name}");
            parts.Add($"Goal: {profile.Goal ?? "General Fitness"}; Experience: {profile.FitnessExperience ?? "Beginner"}; Activity: {profile.ActivityLevel ?? "Moderate"}");
            parts.Add($"Health conditions: {profile.HealthConditions ?? "None"}; Injuries: {profile.Injuries ?? "None"}; Dietary restrictions: {profile.DietaryRestrictions ?? "None"}");

            // ✅ Add workout history
            var workouts = await _context.WorkoutPlans
                .Where(w => w.MemberProfileId == memberProfileId && w.IsActive)
                .Include(w => w.WorkoutDays)
                .ThenInclude(d => d.Exercises)
                .OrderByDescending(w => w.Id)
                .ToListAsync();

            if (workouts.Any())
            {
                var latestWorkout = workouts.First();
                var trainingDays = latestWorkout.WorkoutDays
                    .Take(4)
                    .Select(day =>
                    {
                        var exercises = day.Exercises?
                            .Take(4)
                            .Select(ex => string.IsNullOrWhiteSpace(ex.ExrciseName)
                                ? ex.Exercise?.Name ?? "Exercise"
                                : ex.ExrciseName)
                            .ToList() ?? [];

                        return $"{day.DayName}: {string.Join(", ", exercises)}";
                    })
                    .ToList();

                parts.Add($"Member's current workout plan: {latestWorkout.Name}, " +
                    $"Duration: {latestWorkout.DurationWeeks} weeks");

                if (trainingDays.Any())
                    parts.Add($"Current training split: {string.Join("; ", trainingDays)}");
            }

            // ✅ Add nutrition history
            var nutrition = await _context.NutritionPlans
                .Where(n => n.MemberProfileId == memberProfileId && n.IsActive)
                .Include(n => n.Meals)
                .FirstOrDefaultAsync();

            if (nutrition != null)
            {
                parts.Add($"Member's nutrition plan: {nutrition.DailyCalories} cal/day, " +
                    $"Protein: {nutrition.ProteinGrams}g, " +
                    $"Carbs: {nutrition.CarbsGrams}g, " +
                    $"Fat: {nutrition.FatGrams}g");

                var meals = nutrition.Meals
                    .Take(4)
                    .Select(m => $"{m.MealType}: {m.Name} ({m.Calories} kcal, P {m.Protein}g, C {m.Carbs}g, F {m.Fat}g)")
                    .ToList();

                if (meals.Any())
                    parts.Add($"Current meal examples: {string.Join("; ", meals)}");
            }

            var progress = await _context.ProgressLogs
                .Where(p => p.MemberProfileId == memberProfileId)
                .OrderByDescending(p => p.LoggedAt)
                .Take(3)
                .ToListAsync();

            if (progress.Any())
            {
                var latest = progress.First();
                parts.Add($"Latest progress: {latest.Weight}kg on {latest.LoggedAt:yyyy-MM-dd}" +
                    (latest.BodyFat.HasValue ? $", body fat {latest.BodyFat}%" : "") +
                    (latest.MuscleMass.HasValue ? $", muscle mass {latest.MuscleMass}kg" : ""));
            }

            // ✅ Add booking history
            var bookings = await _context.Bookings
                .Where(b => b.MemberProfileId == memberProfileId
                         && b.Status != ArenaDomain.Enums.BookingStatus.Cancelled)
                .OrderByDescending(b => b.BookingDate)
                .Take(3)
                .ToListAsync();

            if (bookings.Any())
                parts.Add($"Member's recent bookings: " +
                    $"{string.Join(", ", bookings.Select(b => b.BookingDate.ToString("ddd MMM dd")))}");

            return string.Join("\n", parts);
        }

        // ✅ Calculate relevance score
        private static int CalculateScore(KnowledgeChunk chunk, List<string> queryWords)
        {
            var content = NormalizeText(chunk.Content);
            var category = NormalizeText(chunk.Category);
            var score = 0;

            foreach (var word in queryWords)
            {
                // Exact match in content
                if (content.Contains(word)) score += 2;

                // Match in category (higher weight)
                if (category.Contains(word)) score += 3;
            }

            // Boost Arabic keywords
            var arabicKeywords = new Dictionary<string, string[]>
            {
                ["ركبة"] = ["knee"],
                ["ظهر"] = ["back", "lower back"],
                ["كتف"] = ["shoulder"],
                ["اكسب وزن"] = ["muscle gain", "weight gain"],
                ["اخس"] = ["weight loss"],
                ["سكر"] = ["diabetes"],
                ["نوم"] = ["sleep", "recovery"],
                ["قبل التمرين"] = ["pre workout", "pre-workout"],
                ["بعد التمرين"] = ["post workout", "post-workout"],
                ["مبتدئ"] = ["beginner"],
                ["بروتين"] = ["protein"],
                ["سعرات"] = ["calories"],
                ["قلب"] = ["heart", "heart rate"],
                ["مياه"] = ["water", "hydration"],
                ["نشويات"] = ["carbs", "carbohydrates"],
                ["دهون"] = ["fat", "healthy fats"],
                ["استشفاء"] = ["recovery", "soreness"],
                ["تضخيم"] = ["muscle gain", "hypertrophy"],
                ["تنشيف"] = ["fat loss", "weight loss"],
                ["وجبة"] = ["meal", "nutrition"],
                ["أكل"] = ["food", "nutrition"],
                ["اكل"] = ["food", "nutrition"],
            };

            foreach (var (arabic, english) in arabicKeywords)
            {
                if (queryWords.Any(w =>
                    w.Contains(arabic) ||
                    english.Any(e => w.Contains(e))))
                {
                    if (english.Any(e => content.Contains(e)))
                        score += 4;
                }
            }

            return score;
        }

        private static int CalculateScore(KnowledgeChunk chunk, List<string> queryWords, string queryText)
        {
            var content = NormalizeText(chunk.Content);
            var category = NormalizeText(chunk.Category);
            var score = CalculateScore(chunk, queryWords);

            foreach (var phrase in BuildQueryPhrases(queryText))
            {
                if (content.Contains(phrase)) score += 5;
                if (category.Contains(phrase)) score += 8;
            }

            return score;
        }

        private static IEnumerable<string> NormalizeQuery(string query)
        {
            return NormalizeText(query)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2);
        }

        private static string NormalizeText(string value)
        {
            return Regex.Replace(value.ToLowerInvariant(), @"[^\p{L}\p{Nd}]+", " ").Trim();
        }

        private static IEnumerable<string> BuildQueryPhrases(string queryText)
        {
            var phrases = new HashSet<string>();

            AddPhraseIfPresent(phrases, queryText, "pre workout", "before workout", "before training", "قبل التمرين", "قبل الجيم");
            AddPhraseIfPresent(phrases, queryText, "post workout", "after workout", "after training", "بعد التمرين", "بعد الجيم");
            AddPhraseIfPresent(phrases, queryText, "muscle gain", "bulk", "build muscle", "اكسب عضل", "زيادة عضل");
            AddPhraseIfPresent(phrases, queryText, "weight loss", "fat loss", "lose weight", "اخس", "تنشيف");
            AddPhraseIfPresent(phrases, queryText, "hypertrophy", "muscle growth", "تضخيم");
            AddPhraseIfPresent(phrases, queryText, "knee injury", "knee pain", "ركبة");
            AddPhraseIfPresent(phrases, queryText, "lower back", "back pain", "ظهر");
            AddPhraseIfPresent(phrases, queryText, "shoulder pain", "shoulder injury", "كتف");
            AddPhraseIfPresent(phrases, queryText, "protein target", "how much protein", "كام بروتين");
            AddPhraseIfPresent(phrases, queryText, "hydration", "water intake", "مياه", "اشرب مياه");
            AddPhraseIfPresent(phrases, queryText, "heart rate", "قلب");

            return phrases;
        }

        private static void AddPhraseIfPresent(HashSet<string> phrases, string queryText, params string[] aliases)
        {
            if (!aliases.Any(alias => queryText.Contains(NormalizeText(alias))))
                return;

            foreach (var alias in aliases.Select(NormalizeText))
                phrases.Add(alias);
        }

        private static void EnsureKnowledgeLoaded()
        {
            if (_chunks.Any())
                return;

            lock (ChunkLock)
            {
                if (_chunks.Any())
                    return;

                _chunks = LoadKnowledgeChunks();
            }
        }

        private static List<KnowledgeChunk> LoadKnowledgeChunks()
        {
            var chunks = new List<KnowledgeChunk>();

            var possiblePaths = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "AI", "Knowledge", "fitness_knowledge.txt"),
                Path.Combine(Directory.GetCurrentDirectory(), "..", "ArenaApplication", "AI", "Knowledge", "fitness_knowledge.txt"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AI", "Knowledge", "fitness_knowledge.txt"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Knowledge", "fitness_knowledge.txt"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fitness_knowledge.txt")
            };

            string? content = null;
            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    content = File.ReadAllText(path);
                    break;
                }
            }

            if (content == null) return chunks;

            var sections = content.Split("---", StringSplitOptions.RemoveEmptyEntries);

            foreach (var section in sections)
            {
                var trimmed = section.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                var category = trimmed.Split('\n')
                    .FirstOrDefault(l => l.StartsWith("CATEGORY:"))
                    ?.Replace("CATEGORY:", "").Trim() ?? "General";

                chunks.Add(new KnowledgeChunk
                {
                    Content = trimmed,
                    Category = category
                });
            }

            return chunks;
        }


    }


}
