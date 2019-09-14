using Bot3PG.Core.Data;
using Bot3PG.Handlers;
using Bot3PG.Modules;
using Bot3PG.Modules.Music;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Victoria;

namespace Bot3PG.Services
{
    public class DiscordService
    {
        private DiscordSocketClient client;
        private LavaSocketClient lavaSocketClient;
        private ServiceProvider services;
        
        public async Task InitializeAsync()
        {
            services = ConfigureServices();
            lavaSocketClient = services.GetRequiredService<LavaSocketClient>();
            client = services.GetRequiredService<DiscordSocketClient>();

            client = new DiscordSocketClient(new DiscordSocketConfig
            {
                AlwaysDownloadUsers = true,
                LogLevel = LogSeverity.Verbose,
                MessageCacheSize = 100 // increase/decrease cache as needed
            });

            new EventsHandler(services, client, lavaSocketClient);
            new Global(client, lavaSocketClient, GlobalConfig.Config, services.GetRequiredService<CommandService>());

            await client.LoginAsync(TokenType.Bot, Global.Config.Token);
            await client.StartAsync();

            await ValidateBotToken();

            await services.GetRequiredService<CommandHandler>().InitializeAsync();

            await ConsoleCommands.Input();
            await Task.Delay(-1);
        }

        private async Task ValidateBotToken()
        {
            if (string.IsNullOrEmpty(Global.Config.Token))
            {
                await LoggingService.LogCriticalAsync("Bot", "Token is null - Check config");
                Console.ReadKey();
            };
        }

        private ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton<AudioService>()
                .AddSingleton<CommandHandler>()
                .AddSingleton<CommandService>()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<LavaPlayer>()
                .AddSingleton<LavaRestClient>()
                .AddSingleton<LavaShardClient>()
                .AddSingleton<LavaSocketClient>()
                .BuildServiceProvider();
        }
    }
}