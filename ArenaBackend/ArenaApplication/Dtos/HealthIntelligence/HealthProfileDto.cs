using System.Collections.Generic;

namespace ArenaApplication.Dtos.HealthIntelligence
{
    public class HealthProfileDto
    {
        public List<string> Conditions { get; set; } = new();
        public List<string> Allergies { get; set; } = new();
        public List<string> Injuries { get; set; } = new();
        public List<string> Restrictions { get; set; } = new();
        public List<string> Medications { get; set; } = new();
    }
}
