using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Dtos.ProgressLogDtos
{
    public class ProgressLogDto
    {
        public Guid Id { get; set; }
        public decimal Weight { get; set; }
        public decimal? BodyFat { get; set; }
        public decimal? MuscleMass { get; set; }
        public DateTime LoggedAt { get; set; }
    }
}
