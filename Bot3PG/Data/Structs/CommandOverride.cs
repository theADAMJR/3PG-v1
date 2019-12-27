using Discord;

namespace Bot3PG.Data.Structs
{
    public class CommandOverride
    {
        public string Name { get; set; }
        public bool Enabled { get; set; } = true;
        public GuildPermission Permission { get; set; }
    }
}