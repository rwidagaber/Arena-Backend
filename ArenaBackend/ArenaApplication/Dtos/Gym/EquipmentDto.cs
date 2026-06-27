using System;
using System.ComponentModel.DataAnnotations;

namespace ArenaApplication.Dtos.Gym
{
    public class EquipmentDto
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "The Name field is required.")]
        [Display(Name = "Equipment Name")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Arabic Equipment Name")]
        public string? NameAr { get; set; }

        [Required(ErrorMessage = "The Category field is required.")]
        public string Category { get; set; } = string.Empty;

        [Display(Name = "Available")]
        public bool IsAvailable { get; set; } = true;
    }
}
