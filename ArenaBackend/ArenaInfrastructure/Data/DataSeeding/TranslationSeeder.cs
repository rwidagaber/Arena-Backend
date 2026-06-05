using ArenaDomain.Entities.Localization;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace ArenaInfrastructure.Data.DataSeeding;

public static class TranslationSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var resourcesPath = Path.Combine(basePath, "Resources");

        var languages = new[] { "en-US", "ar-EG" };
        var seedDate = DateTime.UtcNow;

        foreach (var lang in languages)
        {
            var filePath = Path.Combine(resourcesPath, $"{lang}.json");
            if (!File.Exists(filePath))
                continue;

            var json = await File.ReadAllTextAsync(filePath);
            var entries = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

            if (entries == null)
                continue;

            var existingKeys = await context.Translations
                .Where(t => t.Language == lang)
                .Select(t => t.Key)
                .ToListAsync();

            var newEntries = entries
                .Where(e => !existingKeys.Contains(e.Key))
                .Select(e => new Translation
                {
                    Id = Guid.NewGuid(),
                    Key = e.Key,
                    Value = e.Value,
                    Language = lang,
                    CreatedAt = seedDate,
                    IsDeleted = false
                })
                .ToList();

            if (newEntries.Count > 0)
            {
                await context.Translations.AddRangeAsync(newEntries);
            }
        }

        await context.SaveChangesAsync();
    }
}
