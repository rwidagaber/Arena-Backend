using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Settings
{
    public class OpenAISettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "llama-3.3-70b-versatile";
    }
}
