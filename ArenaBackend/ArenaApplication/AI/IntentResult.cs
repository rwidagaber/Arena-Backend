using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.AI
{
    namespace ArenaApplication.AI
    {
        public class IntentResult
        {
            public string Intent { get; set; } = "chat";
            public string? Date { get; set; }
            public string? Time { get; set; }
            public string Action { get; set; } = "create";
            public string? RawMessage { get; set; }

            public string? Goal { get; set; }
            public string? Injuries { get; set; }
            public string? HealthConditions { get; set; }
            public string? FitnessExperience { get; set; }
            public string? DietaryRestrictions { get; set; }
            public string? Equipment { get; set; }
            public string? WeightString { get; set; }
            public string? HeightString { get; set; }
            public string? PreferredDuration { get; set; }
        }
    }
}
