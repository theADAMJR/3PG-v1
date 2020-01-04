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
        private DiscordSocketClient bot;
        private LavaSocketClient lavaClient;
        private ServiceProvider services;
        
        public async Task InitializeAsync()
        {
            services = ConfigureServices();
            lavaClient = services.GetRequiredService<LavaSocketClient>();
            bot = services.GetRequiredService<DiscordSocketClient>();

            bot = new DiscordSocketClient(new DiscordSocketConfig
            {
                AlwaysDownloadUsers = true,
                LogLevel = LogSeverity.Verbose,
                MessageCacheSize = 100,
                ExclusiveBulkDelete = true
            });

            new EventsHandler(services, bot, lavaClient);
            new Global(bot, lavaClient, GlobalConfig.Config, services.GetRequiredService<CommandService>());

            await ValidateBotToken();
            await bot.LoginAsync(TokenType.Bot, Global.Config.Token);
            await bot.StartAsync();

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