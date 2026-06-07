using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.AI
{
    public static class AIHelper
    {
        public static string CleanJson(string raw)
        {
            var clean = raw.Trim();

            // Remove markdown
            if (clean.StartsWith("```json"))
                clean = clean.Substring(7);
            else if (clean.StartsWith("```"))
                clean = clean.Substring(3);

            if (clean.EndsWith("```"))
                clean = clean.Substring(0, clean.Length - 3);

            clean = clean.Trim();

            // ✅ خد بس من أول { لحد آخر }
            var jsonStart = clean.IndexOf('{');
            var jsonEnd = clean.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
                clean = clean.Substring(jsonStart, jsonEnd - jsonStart + 1);

            return clean.Trim();
        }
    }
}