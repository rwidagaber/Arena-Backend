namespace ArenaApplication.Dtos.Dashboard.Analytics;

public class AnalyticsEnvelopeDto<T>
{
  public AnalyticsMetaDto Meta { get; set; } = new();
  public T Data { get; set; } = default!;
}
