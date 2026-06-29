using System;
using System.ComponentModel.DataAnnotations;

namespace ArenaApplication.Dtos.Gym
{
    public class EquipmentCategoryDto
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "The Name field is required.")]
        [Display(Name = "Category Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "The Arabic Name field is required.")]
        [Display(Name = "Arabic Category Name")]
        public string NameAr { get; set; } = string.Empty;
    }
}
