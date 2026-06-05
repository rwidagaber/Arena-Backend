using ArenaInfrastructure.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.EntityFrameworkCore;

namespace ArenaInfrastructure.Localization;

public class DbStringLocalizer : IStringLocalizer
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMemoryCache _cache;

    public DbStringLocalizer(IServiceScopeFactory scopeFactory, IMemoryCache cache)
    {
        _scopeFactory = scopeFactory;
        _cache = cache;
    }

    public LocalizedString this[string name]
    {
        get
        {
            var value = GetString(name);
            return new LocalizedString(name, value ?? name, value == null);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var actualValue = this[name];
            return !actualValue.ResourceNotFound
                ? new LocalizedString(name, string.Format(actualValue.Value, arguments))
                : actualValue;
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        var translations = GetAllTranslations();
        var culture = Thread.CurrentThread.CurrentCulture.Name;

        return translations
            .Where(t => t.Language == culture)
            .Select(t => new LocalizedString(t.Key, t.Value, false));
    }

    private string? GetString(string key)
    {
        var translations = GetAllTranslations();
        var culture = Thread.CurrentThread.CurrentCulture.Name;

        return translations
            .FirstOrDefault(t => t.Key == key && t.Language == culture)
            ?.Value;
    }

    private List<TranslationCacheEntry> GetAllTranslations()
    {
        var cacheKey = "AllTranslations";

        if (_cache.TryGetValue(cacheKey, out List<TranslationCacheEntry>? cached))
            return cached!;

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var translations = context.Translations
            .AsNoTracking()
            .Select(t => new TranslationCacheEntry
            {
                Key = t.Key,
                Value = t.Value,
                Language = t.Language
            })
            .ToList();

        _cache.Set(cacheKey, translations, TimeSpan.FromMinutes(5));

        return translations;
    }

    public class TranslationCacheEntry
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
    }
}
