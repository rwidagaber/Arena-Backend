using System;
using System.ComponentModel.DataAnnotations;

namespace ArenaApplication.Dtos.Gym
{
    public class UpdateWorkingHoursDto
    {
        [Required]
        public bool IsClosed { get; set; }

        public TimeSpan? OpenTime { get; set; }

        public TimeSpan? CloseTime { get; set; }
    }
}
