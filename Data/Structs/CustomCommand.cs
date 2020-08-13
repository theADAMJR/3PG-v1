using Discord;

namespace Bot3PG.Data.Structs
{
    public struct CustomCommand
    {
        public string Name { get; set; }
        public string[] Aliases { get; set; }
        public GuildPermission Permission { get; set; }
    }
}