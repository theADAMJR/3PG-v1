using Bot3PG.Modules;
using Discord;

namespace Bot3PG.Data.Structs
{
    public class CommandOverride
    {
        [Config("Name of the existing command"), SpecialType(typeof(Command))]
        public string Name { get; set; }

        [Config("Whether the command can be used")]
        public bool Enabled { get; set; } = true;

        [Config("The minimum permission to use the command"), Dropdown(typeof(GuildPermission))]
        public GuildPermission Permission { get; set; }
    }
}