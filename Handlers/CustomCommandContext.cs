using Bot3PG.Data.Structs;
using Discord.Commands;
using Discord.WebSocket;

namespace Bot3PG.Handlers
{
    public class CustomCommandContext : SocketCommandContext
    {
        public int ExecutionTime { get; set; }
        public Guild CurrentGuild { get; set; }

        public CustomCommandContext(DiscordSocketClient client, SocketUserMessage msg) : base(client, msg) {}
    }
}