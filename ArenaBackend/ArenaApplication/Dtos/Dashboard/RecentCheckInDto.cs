namespace ArenaApplication.Dtos.Dashboard
{
    public class RecentCheckInDto
    {
        public string MemberName { get; set; } = string.Empty;
        public string Initials { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public DateTime CheckInTime { get; set; }
        public string AvatarColor { get; set; } = "#8B5CF6";

        /// <summary>
        /// Returns a human-readable relative time string (e.g., "10 mins ago").
        /// </summary>
        public string TimeAgo
        {
            get
            {
                var diff = DateTime.UtcNow - CheckInTime;

                if (diff.TotalMinutes < 1)
                    return "Just now";
                if (diff.TotalMinutes < 60)
                    return $"{(int)diff.TotalMinutes} mins ago";
                if (diff.TotalHours < 24)
                    return $"{(int)diff.TotalHours} hrs ago";

                return $"{(int)diff.TotalDays} days ago";
            }
        }
    }
}
