using ArenaDomain.Entities.Gym;
using ArenaDomain.Entities.Workout;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ArenaInfrastructure.Data.DataSeeding
{
    /// <summary>
    /// One-shot seeder that backfills missing localization columns for three tables:
    ///   • Equipments          — Name / NameAr
    ///   • ExerciseCatalogItems — Name / NameAr, Description / DescriptionAr, MuscleGroup / MuscleGroupAr
    ///   • Exercises            — Name / NameAr, Description / DescriptionAr, MuscleGroup / MuscleGroupAr, Equipment / EquipmentAr
    ///
    /// Uses the MyMemory free translation REST API — no API key required.
    /// https://mymemory.translated.net/doc/spec.php
    ///
    /// Deduplication: each unique (text, from, to) combination is translated only once
    /// and reused across all rows, dramatically reducing the number of HTTP calls.
    ///
    /// One-time guarantee: each section only queries rows with at least one null
    /// localization column, so after a successful run the seeder completes in milliseconds.
    /// </summary>
    public static class ExerciseLocalizationSeeder
    {
        private const string MyMemoryUrl = "https://api.mymemory.translated.net/get";

        public static async Task SeedAsync(AppDbContext context)
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

            // Shared translation cache across all three tables — (text, from, to) → translation
            var cache = new TranslationCache();

            await SeedEquipmentsAsync(context, http, cache);
            await SeedExerciseCatalogAsync(context, http, cache);
            await SeedExercisesAsync(context, http, cache);

            Console.WriteLine($"[LocalizationSeeder] All tables done. Total unique API calls: {cache.Count}.");
        }

        // ── Equipments ────────────────────────────────────────────────────────

        private static async Task SeedEquipmentsAsync(AppDbContext context, HttpClient http, TranslationCache cache)
        {
            var rows = await context.Equipments
                .Where(e => e.NameAr == null)
                .ToListAsync();

            if (rows.Count == 0)
            {
                Console.WriteLine("[LocalizationSeeder][Equipments] Already localized. Skipping.");
                return;
            }

            Console.WriteLine($"[LocalizationSeeder][Equipments] Translating {rows.Count} row(s)...");
            int ok = 0, fail = 0;

            foreach (var row in rows)
            {
                bool isArabic = IsArabicText(row.Name);
                try
                {
                    if (isArabic)
                    {
                        row.NameAr = row.Name;
                        row.Name   = await cache.Translate(http, row.Name, "ar", "en") ?? row.Name;
                    }
                    else
                    {
                        row.NameAr ??= await cache.Translate(http, row.Name, "en", "ar");
                    }
                    context.Equipments.Update(row);
                    ok++;
                    Console.WriteLine($"[LocalizationSeeder][Equipments] ✔ {row.Name} → {row.NameAr}");
                }
                catch (Exception ex) { fail++; Console.WriteLine($"[LocalizationSeeder][Equipments] ⚠ Id={row.Id}: {ex.Message}"); }
            }

            await context.SaveChangesAsync();
            Console.WriteLine($"[LocalizationSeeder][Equipments] Done — ✔{ok} ⚠{fail}.");
        }

        // ── ExerciseCatalogItems ──────────────────────────────────────────────

        private static async Task SeedExerciseCatalogAsync(AppDbContext context, HttpClient http, TranslationCache cache)
        {
            var rows = await context.ExerciseCatalogItems
                .Where(e => e.NameAr == null || e.DescriptionAr == null || e.MuscleGroupAr == null)
                .ToListAsync();

            if (rows.Count == 0)
            {
                Console.WriteLine("[LocalizationSeeder][ExerciseCatalog] Already localized. Skipping.");
                return;
            }

            Console.WriteLine($"[LocalizationSeeder][ExerciseCatalog] Translating {rows.Count} row(s)...");
            int ok = 0, fail = 0;

            foreach (var row in rows)
            {
                bool isArabic = IsArabicText(row.Name);
                try
                {
                    if (isArabic)
                    {
                        row.NameAr        ??= row.Name;
                        row.DescriptionAr ??= row.Description;
                        row.MuscleGroupAr ??= row.MuscleGroup;

                        row.Name        = await cache.Translate(http, row.NameAr,        "ar", "en") ?? row.Name;
                        row.Description = await cache.Translate(http, row.DescriptionAr, "ar", "en") ?? row.Description;
                        row.MuscleGroup = await cache.Translate(http, row.MuscleGroupAr, "ar", "en") ?? row.MuscleGroup;
                    }
                    else
                    {
                        row.NameAr        ??= await cache.Translate(http, row.Name,        "en", "ar");
                        row.DescriptionAr ??= await cache.Translate(http, row.Description, "en", "ar");
                        row.MuscleGroupAr ??= await cache.Translate(http, row.MuscleGroup, "en", "ar");
                    }
                    context.ExerciseCatalogItems.Update(row);
                    ok++;
                    Console.WriteLine($"[LocalizationSeeder][ExerciseCatalog] ✔ {row.Name} → {row.NameAr}");
                }
                catch (Exception ex) { fail++; Console.WriteLine($"[LocalizationSeeder][ExerciseCatalog] ⚠ Id={row.Id}: {ex.Message}"); }
            }

            await context.SaveChangesAsync();
            Console.WriteLine($"[LocalizationSeeder][ExerciseCatalog] Done — ✔{ok} ⚠{fail}.");
        }

        // ── Exercises ─────────────────────────────────────────────────────────

        private static async Task SeedExercisesAsync(AppDbContext context, HttpClient http, TranslationCache cache)
        {
            var rows = await context.Exercises
                .Where(e => e.NameAr == null || e.MuscleGroupAr == null || e.DescriptionAr == null || e.EquipmentAr == null)
                .ToListAsync();

            if (rows.Count == 0)
            {
                Console.WriteLine("[LocalizationSeeder][Exercises] Already localized. Skipping.");
                return;
            }

            Console.WriteLine($"[LocalizationSeeder][Exercises] Translating {rows.Count} row(s)...");
            int ok = 0, fail = 0;

            foreach (var row in rows)
            {
                bool isArabic = IsArabicText(row.Name);
                try
                {
                    if (isArabic)
                    {
                        row.NameAr        ??= row.Name;
                        row.DescriptionAr ??= row.Description;
                        row.MuscleGroupAr ??= row.MuscleGroup;
                        row.EquipmentAr   ??= row.Equipment;

                        row.Name        = await cache.Translate(http, row.NameAr,        "ar", "en") ?? row.Name;
                        row.Description = await cache.Translate(http, row.DescriptionAr, "ar", "en") ?? row.Description;
                        row.MuscleGroup = await cache.Translate(http, row.MuscleGroupAr, "ar", "en") ?? row.MuscleGroup;
                        row.Equipment   = await cache.Translate(http, row.EquipmentAr,   "ar", "en") ?? row.Equipment;
                    }
                    else
                    {
                        row.NameAr        ??= await cache.Translate(http, row.Name,        "en", "ar");
                        row.DescriptionAr ??= await cache.Translate(http, row.Description, "en", "ar");
                        row.MuscleGroupAr ??= await cache.Translate(http, row.MuscleGroup, "en", "ar");
                        row.EquipmentAr   ??= await cache.Translate(http, row.Equipment,   "en", "ar");
                    }
                    context.Exercises.Update(row);
                    ok++;
                    Console.WriteLine($"[LocalizationSeeder][Exercises] ✔ {row.Name} → {row.NameAr}");
                }
                catch (Exception ex) { fail++; Console.WriteLine($"[LocalizationSeeder][Exercises] ⚠ Id={row.Id}: {ex.Message}"); }
            }

            await context.SaveChangesAsync();
            Console.WriteLine($"[LocalizationSeeder][Exercises] Done — ✔{ok} ⚠{fail}.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static bool IsArabicText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            int arabicCount = text.Count(c => c >= '\u0600' && c <= '\u06FF');
            int latinCount  = text.Count(c => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'));
            return arabicCount > latinCount;
        }

        // ── Translation cache + MyMemory caller ──────────────────────────────

        private sealed class TranslationCache
        {
            private readonly Dictionary<(string, string, string), string> _cache = new(new TupleComparer());
            public int Count => _cache.Count;

            public async Task<string?> Translate(HttpClient http, string? text, string from, string to)
            {
                if (string.IsNullOrWhiteSpace(text)) return null;

                var key = (text.Trim(), from, to);
                if (_cache.TryGetValue(key, out var cached)) return cached;

                try
                {
                    var url = $"https://api.mymemory.translated.net/get?q={Uri.EscapeDataString(text.Trim())}&langpair={from}|{to}";
                    var result = await http.GetFromJsonAsync<MyMemoryResponse>(url);

                    if (result?.ResponseStatus == 200 && !string.IsNullOrWhiteSpace(result.ResponseData?.TranslatedText))
                    {
                        _cache[key] = result.ResponseData.TranslatedText;
                        await Task.Delay(120); // courtesy delay for free tier
                        return result.ResponseData.TranslatedText;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[LocalizationSeeder] ⚠ Translation failed for '{text}': {ex.Message}");
                }

                return null;
            }

            private sealed class TupleComparer : IEqualityComparer<(string, string, string)>
            {
                public bool Equals((string, string, string) x, (string, string, string) y)
                    => string.Equals(x.Item1, y.Item1, StringComparison.OrdinalIgnoreCase)
                    && x.Item2 == y.Item2 && x.Item3 == y.Item3;

                public int GetHashCode((string, string, string) obj)
                    => HashCode.Combine(obj.Item1.ToLowerInvariant(), obj.Item2, obj.Item3);
            }
        }

        // ── MyMemory response models ──────────────────────────────────────────

        private sealed class MyMemoryResponse
        {
            [JsonPropertyName("responseData")]
            public MyMemoryResponseData? ResponseData { get; set; }

            [JsonPropertyName("responseStatus")]
            public int ResponseStatus { get; set; }
        }

        private sealed class MyMemoryResponseData
        {
            [JsonPropertyName("translatedText")]
            public string? TranslatedText { get; set; }
        }
    }
}
