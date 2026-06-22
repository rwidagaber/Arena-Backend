namespace ArenaApplication.Dtos.Attendance
{
    /// <summary>Crowd level for a single open hour of the gym day (local time).</summary>
    public class OccupancySlotDto
    {
        /// <summary>Hour of day in 24h local (gym) time, e.g. 18 = 6 PM.</summary>
        public int Hour { get; set; }

        /// <summary>Historical check-ins on this weekday at this hour.</summary>
        public int CheckInCount { get; set; }

        /// <summary>Bookings already placed on the target date at this hour.</summary>
        public int BookingCount { get; set; }

        /// <summary>Combined load used to rank how busy the hour is.</summary>
        public int Total { get; set; }

        /// <summary>True when bookings in this hour exceed slot capacity (render red / full).</summary>
        public bool OverCapacity { get; set; }

        /// <summary>Relative crowd level: Low | Medium | High (High when over capacity).</summary>
        public string Level { get; set; } = "Low";
    }
}
