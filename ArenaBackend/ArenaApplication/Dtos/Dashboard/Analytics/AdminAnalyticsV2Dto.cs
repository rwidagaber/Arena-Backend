namespace ArenaApplication.Dtos.Dashboard.Analytics;

public class AdminAnalyticsV2Dto
{
  public ExecutiveKpisDto Executive { get; set; } = new();
  public FinancialAnalyticsDto Financial { get; set; } = new();
  public OperationalAnalyticsDto Operational { get; set; } = new();
}
