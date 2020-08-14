using Discord;

namespace Bot3PG.Data.Structs
{
    public class Config
    {
        public struct BotData {
            public string Token { get; set; }
            public string Status { get; set; }
        }
        public BotData Bot { get; set; }
        public string DashboardURL { get; set; }
        public LogSeverity LogSeverity { get; set; } = LogSeverity.Verbose;
        public string MongoURI { get; set; }
        
    }
}