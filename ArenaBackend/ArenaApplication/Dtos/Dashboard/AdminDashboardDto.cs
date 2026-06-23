namespace ArenaApplication.Dtos.Dashboard
{
    public class AdminDashboardDto
    {
        // ── KPI Cards ──────────────────────────────────────────────────────────
        public int TotalMembers { get; set; }
        public int ActiveSubscriptions { get; set; }
        public int ExpiringSubscriptions { get; set; }
        public int TodayAttendance { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public int ActivePlans { get; set; }
        public int TotalPlans { get; set; }
        public int MembersWithoutActiveSubscriptions { get; set; }

        // ── Growth Percentages (vs previous period) ────────────────────────────
        public decimal MemberGrowthPercent { get; set; }
        public decimal SubscriptionGrowthPercent { get; set; }
        public decimal RevenueGrowthPercent { get; set; }

        // ── Weekly Attendance Chart Data ────────────────────────────────────────
        public List<DailyAttendanceDto> WeeklyAttendance { get; set; } = [];

        // ── Recent Check-ins ───────────────────────────────────────────────────
        public List<RecentCheckInDto> RecentCheckIns { get; set; } = [];
    }
}
