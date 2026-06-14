namespace ArenaApplication.Dtos.Dashboard.Analytics;

public class AnalyticsQueryWindowDto
{
  public DateTime? StartDateUtc { get; set; }
  public DateTime? EndDateUtc { get; set; }
  public string Timezone { get; set; } = "UTC";
}
