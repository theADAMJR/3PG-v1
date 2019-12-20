using Bot3PG.Data.Structs;
using Bot3PG.Modules.General;
using Discord.Commands;
using Discord.WebSocket;
using System;
using Victoria;

namespace Bot3PG
{
    public class Global
    {
        public static DiscordSocketClient Client { get; private set; }
        public static LavaSocketClient Lavalink { get; private set; }
        public static Config Config { get; private set; }
        public static Config.DatabaseConfig DatabaseConfig { get; private set; }
        public static CommandService CommandService { get; private set; }

        private static readonly DateTime _startTime = DateTime.Now;
        public static TimeSpan Uptime => DateTime.Now - _startTime;

        public Global(DiscordSocketClient discordSocketClient, LavaSocketClient lavaSocketClient, Config config, CommandService commandService)
        {
            Client = discordSocketClient;
            Lavalink = lavaSocketClient;
            Config = config;
            CommandService = commandService;
            DatabaseConfig = config.DB;
        }
    }
}