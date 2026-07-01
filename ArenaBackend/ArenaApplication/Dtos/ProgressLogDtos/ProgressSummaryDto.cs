using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Dtos.ProgressLogDtos
{
    public class ProgressSummaryDto
    {
        public decimal CurrentWeight { get; set; }
        public decimal? CurrentBodyFat { get; set; }
        public decimal? CurrentMuscleMass { get; set; }
        public decimal? WeightChange { get; set; }   // vs last log
        public decimal? BodyFatChange { get; set; }
        public decimal? MuscleMassChange { get; set; }
        public List<ProgressLogDto> Logs { get; set; } = [];
    }
}
