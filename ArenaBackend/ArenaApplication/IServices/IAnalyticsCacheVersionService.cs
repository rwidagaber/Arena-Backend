namespace ArenaApplication.IServices;

public interface IAnalyticsCacheVersionService
{
  long GetVersion();
  long BumpVersion();
}
