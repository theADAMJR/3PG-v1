using Bot3PG.Data;
using Bot3PG.Handlers;
using Bot3PG.Modules.Music;
using Bot3PG.Services;
using Discord;
using Discord.Commands;
using Discord.Webhook;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Victoria;

namespace Bot3PG
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
                LogLevel = LogSeverity.Debug,
                MessageCacheSize = 100,
                ExclusiveBulkDelete = true
            });

            new EventsHandler(services, client, lavaSocketClient);
            new Global(client, lavaSocketClient, GlobalConfig.Config, services.GetRequiredService<CommandService>());
            new DatabaseManager();

            await ValidateBotToken();
            await client.LoginAsync(TokenType.Bot, Global.Config.Token);
            await client.StartAsync();

            await services.GetRequiredService<CommandHandler>().InitializeAsync();

            await Task.Delay(-1);
        }

        private async Task ValidateBotToken()
        {
            if (string.IsNullOrEmpty(Global.Config.Token))
            {
                await Debug.LogCriticalAsync("Bot", "Token is null - Check config");
                Console.ReadKey();
            }
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