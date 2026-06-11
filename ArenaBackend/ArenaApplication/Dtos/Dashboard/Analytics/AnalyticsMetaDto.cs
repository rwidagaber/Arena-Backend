namespace ArenaApplication.Dtos.Dashboard.Analytics;

public class AnalyticsMetaDto
{
  public DateTime GeneratedAtUtc { get; set; }
  public DateTime DataAsOfUtc { get; set; }
  public DateTime StartDateUtc { get; set; }
  public DateTime EndDateUtc { get; set; }
  public string Timezone { get; set; } = "UTC";
  public List<string> DataQualityFlags { get; set; } = [];
}
