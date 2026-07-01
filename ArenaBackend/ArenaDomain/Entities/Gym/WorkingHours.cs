using ArenaDomain.Enums;
using ArenaDomain.Shared;
using System;

namespace ArenaDomain.Entities.Gym
{
    public class WorkingHours : BaseEntity<int>
    {
        public WorkingDay DayOfWeek { get; set; }
        public TimeSpan OpenTime { get; set; }
        public TimeSpan CloseTime { get; set; }
        public bool IsClosed { get; set; }
    }
}
