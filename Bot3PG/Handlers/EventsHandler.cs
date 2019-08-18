using System;
using System.Threading.Tasks;
using Bot3PG.Core.Data;
using Bot3PG.DataStructs;
using Bot3PG.Modules;
using Bot3PG.Modules.General;
using Bot3PG.Modules.Moderation;
using Bot3PG.Modules.Music;
using Bot3PG.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Victoria;

namespace Bot3PG.Handlers
{
    // TODO - finish implementation
    public class EventsHandler
    {
        private readonly Announce announce = new Announce();
        private readonly AutoModeration autoModeration = new AutoModeration();
        private readonly ConsoleCommands consoleCommands = new ConsoleCommands();
        private readonly Rulebox rulebox = new Rulebox();
        private readonly StaffLogs staffLogs = new StaffLogs();

        private DiscordSocketClient socketClient;
        private ServiceProvider services;
        private LavaSocketClient lavaSocketClient;

        public EventsHandler(ServiceProvider services, DiscordSocketClient socketClient, LavaSocketClient lavaSocketClient)
        {
            this.socketClient = socketClient;
            this.services = services;
            this.lavaSocketClient = lavaSocketClient;
            HookEvents();
        }

        public void HookEvents()
        {
            lavaSocketClient.Log += LogAsync;
            services.GetRequiredService<CommandService>().Log += LogAsync;

            socketClient.Log += LogAsync;
            socketClient.Ready += async () =>
            {
                try
                {
                    await lavaSocketClient.StartAsync(socketClient);
                    lavaSocketClient.OnTrackFinished += services.GetService<AudioService>().OnFinished;
                    await socketClient.SetGameAsync(Global.Config.GameStatus);
                }
                catch (Exception ex)
                {
                    await LoggingService.LogInformationAsync(ex.Source, ex.Message);
                }
            };

            socketClient.JoinedGuild += OnGuildJoined;

            socketClient.ReactionAdded += OnReactionAdded;
            socketClient.ReactionRemoved += OnReactionRemoved;

            socketClient.MessageReceived += autoModeration.OnMessageRecieved;
            socketClient.MessageDeleted += staffLogs.LogMessageDeletion;
            //socketClient.MessagesBulkDeleted += OnMessagesBulkDeleted;

            socketClient.UserJoined += OnUserJoined;
            socketClient.UserLeft += OnUserLeft;
            socketClient.UserLeft += rulebox.RemoveUserReaction;
            socketClient.UserLeft += UserKicked;
            socketClient.UserBanned += staffLogs.LogBan;
            socketClient.UserUnbanned += staffLogs.OnUserUnbanned;

            GuildUser.GuildUserUpdated += Users.OnUserUpdated;
        }

        #region General Events
        private async Task LogAsync(LogMessage logMessage) => await LoggingService.LogAsync(logMessage.Source, logMessage.Severity, logMessage.Message);
        #endregion General Events

        #region Reaction Events
        private async Task OnGuildJoined(SocketGuild socketGuild)
        {
            var newGuild = await Guilds.GetAsync(socketGuild);
            var channel = socketGuild.SystemChannel ?? socketGuild.DefaultChannel;

            var embed = new EmbedBuilder();
            embed.WithTitle($"Hi, I'm {socketClient.CurrentUser.Username}.");
            embed.AddField("⚙️ Config", $"Type {newGuild.Config.CommandPrefix}config to customize me for your server's needs.", inline: true);
            embed.AddField("📜 Commands", $"Type {newGuild.Config.CommandPrefix}help for a list of commands.", inline: true);
            embed.AddField("❔ Support", $"Need help with {socketClient.CurrentUser.Username}? Join our discord for more support: {Global.Config.WelcomeLink}", inline: true);

            await channel.SendMessageAsync("", embed: embed.Build());
        }

        public async Task OnReactionAdded(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (!cache.HasValue) return;
            var socketGuildUser = cache.Value.Author as SocketGuildUser;
            var guild = await Guilds.GetAsync(socketGuildUser.Guild);
            // TODO - add specific module staff log events config
            if (guild is null || !guild.Config.RuleboxEnabled) return;

            await rulebox.AgreedToRules(cache, channel, reaction);
        }

        public async Task OnReactionRemoved(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var socketGuild = (channel as SocketTextChannel).Guild;
            var guild = await Guilds.GetAsync(socketGuild);
            if (guild is null || !guild.Config.RuleboxEnabled) return;

            await rulebox.OnReactionRemoved(cache, channel, reaction);
        }
        #endregion Reaction Events

        #region Announce Events
        public async Task OnUserJoined(SocketGuildUser socketGuildUser)
        {
            var guild = await Guilds.GetAsync(socketGuildUser.Guild);
            if (guild is null || !guild.Config.AnnounceEnabled) return;

            await announce.AnnounceUserJoin(socketGuildUser);
        }

        public async Task OnUserLeft(SocketGuildUser socketGuildUser)
        {
            var guild = await Guilds.GetAsync(socketGuildUser.Guild);

            if (guild is null || !guild.Config.AnnounceEnabled) return;

            await announce.AnnounceUserLeft(socketGuildUser);
        }

        #endregion Announce Events

        public async Task OnUserWarned()
        {

        }

        #region User Events

        public async Task UserKicked(SocketGuildUser socketGuildUser)
        {
            await staffLogs.LogKick(socketGuildUser);
        }

        #endregion User Events

    }
}