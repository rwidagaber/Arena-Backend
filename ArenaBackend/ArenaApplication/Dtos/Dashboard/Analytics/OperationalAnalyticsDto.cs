namespace ArenaApplication.Dtos.Dashboard.Analytics;

public class OperationalAnalyticsDto
{
  public List<DailyMetricPointDto> DailyAttendance { get; set; } = [];
  public int BookingsInWindow { get; set; }
  public int CheckInsInWindow { get; set; }
  public int CompletedSessionsInWindow { get; set; }
}
