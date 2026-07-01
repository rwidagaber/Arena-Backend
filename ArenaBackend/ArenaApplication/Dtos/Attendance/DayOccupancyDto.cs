using System;
using System.Collections.Generic;

namespace ArenaApplication.Dtos.Attendance
{
    /// <summary>
    /// Per-hour occupancy profile for the gym on a given date, scoped to that
    /// day's working hours. Empty slots when the gym is closed.
    /// </summary>
    public class DayOccupancyDto
    {
        public DateTime Date { get; set; }

        public string DayOfWeek { get; set; } = string.Empty;

        public bool IsClosed { get; set; }

        public TimeSpan? OpenTime { get; set; }

        public TimeSpan? CloseTime { get; set; }

        public List<OccupancySlotDto> Slots { get; set; } = new();
    }
}
