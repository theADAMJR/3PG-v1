using Bot3PG.Handlers;
using Bot3PG.Modules;
using Bot3PG.Services;
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
        private Lavalink lavaLink;
        private ServiceProvider services;

        private readonly Announce announce = new Announce();
        private readonly AutoModeration autoModeration = new AutoModeration();
        private readonly ConsoleCommands consoleCommands = new ConsoleCommands();
        private readonly Rulebox rulebox = new Rulebox();
        private readonly StaffLogs staffLogs = new StaffLogs();
        
        public async Task InitializeAsync()
        {
            services = ConfigureServices();
            lavaLink = services.GetRequiredService<Lavalink>();
            client = services.GetRequiredService<DiscordSocketClient>();

            await ValidateBotToken();

            client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose,
                AlwaysDownloadUsers = true,
                MessageCacheSize = 100 // increase/decrease cache as needed
            });

            HookEvents();

            Global.Client = client;
            Global.RuleboxMessageID = Global.Config.RuleboxMessageID;
            Global.InitializeStartTime();

            await client.LoginAsync(TokenType.Bot, Global.Config.Token);
            await client.StartAsync();

            await services.GetRequiredService<CommandHandler>().InitializeAsync();

            await ConsoleCommands.Input();
            await Task.Delay(-1);
        }

        private Task ValidateBotToken()
        {
            var config = new GlobalConfig();
            if (Global.Config.Token == "" || Global.Config.Token == null)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("Token is null - Check config");
                Console.ReadKey();
            };
            return Task.CompletedTask;
        }
        
        private void HookEvents()
        {
            lavaLink.Log += LogAsync;
            services.GetRequiredService<CommandService>().Log += LogAsync;
            client.Log += LogAsync;
            client.Ready += OnReadyAsync;

            client.ReactionAdded += rulebox.OnReactionAdded;
            client.ReactionRemoved += rulebox.OnReactionRemoved;

            client.MessageReceived += autoModeration.OnMessageRecieved;
            client.MessageDeleted += staffLogs.OnMessageDeleted;

            client.UserJoined += announce.OnUserJoined;
            client.UserLeft += announce.OnUserLeft;
            client.UserBanned += staffLogs.OnUserBanned;
            client.UserUnbanned += staffLogs.OnUserUnbanned;
        }
        
        private async Task OnReadyAsync()
        {
            try
            {
                var node = await lavaLink.AddNodeAsync(client, new Configuration
                {
                    Severity = LogSeverity.Info
                });
                node.TrackFinished += services.GetService<AudioService>().OnFinished;
                await client.SetGameAsync(Global.Config.GameStatus);
            }
            catch (Exception ex)
            {
                await LoggingService.LogInformationAsync(ex.Source, ex.Message);
            }
        }

        private async Task LogAsync(LogMessage logMessage)
        {
            await LoggingService.LogAsync(logMessage.Source, logMessage.Severity, logMessage.Message);
        }
        
        private ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandler>()
                .AddSingleton<Lavalink>()
                .AddSingleton<AudioService>()
                .BuildServiceProvider();
        }
    }
}