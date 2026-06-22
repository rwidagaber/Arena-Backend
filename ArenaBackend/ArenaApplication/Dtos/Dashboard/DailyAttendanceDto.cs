namespace ArenaApplication.Dtos.Dashboard
{
    public class DailyAttendanceDto
    {
        public string DayName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }
}
