using ArenaDomain.Shared;
using System;

namespace ArenaDomain.Entities.Gym
{
    public class Equipment : BaseEntity<Guid>
    {
        public string Name { get; set; } = string.Empty;
        public string? NameAr { get; set; }
        public string Category { get; set; } = string.Empty; // e.g., "Cardio", "Strength", "Free Weights"
        public bool IsAvailable { get; set; } = true;
    }
}
