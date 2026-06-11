using ArenaApplication.IServices;
using System.Threading;

namespace ArenaInfrastructure.Services;

public class AnalyticsCacheVersionService : IAnalyticsCacheVersionService
{
  private long _version = 1;

  public long GetVersion() => Interlocked.Read(ref _version);

  public long BumpVersion() => Interlocked.Increment(ref _version);
}
