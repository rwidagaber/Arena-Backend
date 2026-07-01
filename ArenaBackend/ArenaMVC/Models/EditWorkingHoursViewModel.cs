using ArenaDomain.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace ArenaMVC.Models
{
    public class EditWorkingHoursViewModel
    {
        public int? Id { get; set; }
        
        public WorkingDay DayOfWeek { get; set; }

        [Display(Name = "IsClosed")]
        public bool IsClosed { get; set; }

        [Display(Name = "OpenTime")]
        public TimeSpan? OpenTime { get; set; }

        [Display(Name = "CloseTime")]
        public TimeSpan? CloseTime { get; set; }
    }
}
