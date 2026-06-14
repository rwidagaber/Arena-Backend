namespace ArenaApplication.Dtos.Dashboard.Analytics;

public class FinancialAnalyticsDto
{
  public decimal RevenueInWindow { get; set; }
  public decimal RevenueGrowthPercent { get; set; }
  public List<DailyMetricPointDto> DailyRevenue { get; set; } = [];
}
