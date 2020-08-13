using Discord;

namespace Bot3PG.Data.Structs
{
    public class Config
    {
        public struct Bot {
            public string Token { get; set; }
            public string Status { get; set; }
        }
        public string DashboardURL { get; set; }
        public LogSeverity LogSeverity { get; set; } = LogSeverity.Verbose;
        public string MongoURI { get; set; }
    }
}