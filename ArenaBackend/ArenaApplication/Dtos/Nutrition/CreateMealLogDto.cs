using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Dtos.Nutrition
{
    public class CreateMealLogDto
    {
        public string ImageUrl { get; set; } = string.Empty; // ← URL من Cloudinary
    }
}
