namespace ArenaApplication.Dtos.Dashboard.Analytics;

public class ExecutiveKpisDto
{
  public int TotalMembers { get; set; }
  public int ActiveSubscriptions { get; set; }
  public int ExpiringSubscriptionsNext7Days { get; set; }
  public int AttendanceToday { get; set; }
}
