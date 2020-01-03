using Discord;

namespace Bot3PG.Data.Structs
{
    public class Config
    {
        public string Token { get; set; }
        public string GameStatus { get; set; }
        public string WebappLink { get; set; }
        public string Emote { get; set; }
        public LogSeverity LogSeverity { get; set; } = LogSeverity.Verbose;

        public DatabaseConfig DB { get; set; } = new DatabaseConfig();

        public struct DatabaseConfig
        {
            public string Server { get; set; }
            public int Port { get; set; }
            public string AuthDatabase { get; set; }
            public string Database { get; set; }
            public string User { get; set; }
            public string Password { get; set; }
        }
    }
}