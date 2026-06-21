using ArenaDomain.Enums;
using System;

namespace ArenaApplication.Dtos.Gym
{
    public class WorkingHoursDto
    {
        public int Id { get; set; }
        public WorkingDay DayOfWeek { get; set; }
        public TimeSpan? OpenTime { get; set; }
        public TimeSpan? CloseTime { get; set; }
        public bool IsClosed { get; set; }
    }
}
