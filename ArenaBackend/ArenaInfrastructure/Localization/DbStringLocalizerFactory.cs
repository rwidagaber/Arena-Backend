using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace ArenaInfrastructure.Localization;

public class DbStringLocalizerFactory : IStringLocalizerFactory
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMemoryCache _cache;

    public DbStringLocalizerFactory(IServiceScopeFactory scopeFactory, IMemoryCache cache)
    {
        _scopeFactory = scopeFactory;
        _cache = cache;
    }

    public IStringLocalizer Create(Type resourceSource)
    {
        return new DbStringLocalizer(_scopeFactory, _cache);
    }

    public IStringLocalizer Create(string baseName, string location)
    {
        return new DbStringLocalizer(_scopeFactory, _cache);
    }
}
