using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.settings
{
    public class OpenAISettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string MainModel { get; set; } = "llama-3.3-70b-versatile";
        public string FastModel { get; set; } = "llama-3.1-8b-instant";
    }
}
