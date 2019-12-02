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
    public class EventsHandler
    {
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
            HookLogEvents();
            HookClientEvents();
            HookMessageEvents();
            HookUserEvents();
        }

        private void HookLogEvents()
        {
            lavaSocketClient.Log += async (LogMessage message) => await Debug.LogAsync(message.Source, message.Severity, message.Message);
            services.GetRequiredService<CommandService>().Log += async (LogMessage message) => await Debug.LogAsync(message.Source, message.Severity, message.Message);
            socketClient.Log += async (LogMessage message) => await Debug.LogAsync(message.Source, message.Severity, message.Message);
        }

        private void HookClientEvents()
        {
            socketClient.Ready += async () =>
            {
                try
                {
                    await lavaSocketClient.StartAsync(socketClient);
                    lavaSocketClient.OnTrackFinished += services.GetService<AudioService>().OnFinished;
                    await socketClient.SetGameAsync(Global.Config.GameStatus);

                    await new DatabaseManager().UpdateCommands(new CommandHelp());
                }
                catch (Exception ex)
                {
                    await Debug.LogInformationAsync(ex.Source, ex.Message);
                }
            };

            socketClient.JoinedGuild += async (SocketGuild socketGuild) =>
            {
                var newGuild = await Guilds.GetAsync(socketGuild);
                var channel = socketGuild.SystemChannel ?? socketGuild.DefaultChannel;

                var embed = new EmbedBuilder();
                embed.WithTitle($"Hi, I'm {socketClient.CurrentUser.Username}.");
                embed.AddField("⚙️ Config", $"Customize me to your server's needs at {Global.Config.WebappLink}/servers/{socketGuild.Id}", inline: true);
                embed.AddField("📜 Commands", $"Type {newGuild.General.CommandPrefix}help for a list of commands.", inline: true);
                embed.AddField("❔ Support", $"Need help with {socketClient.CurrentUser.Username}? Join our Discord for more support: {Global.Config.WebappLink}/support", inline: true);

                await channel.SendMessageAsync(embed: embed.Build());
            };
        }

        private void HookMessageEvents()
        {
            socketClient.ReactionAdded += async (Cacheable<IUserMessage, ulong> before, ISocketMessageChannel channel, SocketReaction reaction) =>
            {
                var socketGuildUser = reaction.User.Value as SocketGuildUser;
                var guild = await Guilds.GetAsync(socketGuildUser?.Guild);
                if (guild is null) return;

                // TODO - add specific module staff log events config
                if (guild.Admin.Rulebox.Enabled)
                {
                    await Rulebox.AgreedToRules(guild, socketGuildUser, reaction);
                }
            };

            socketClient.ReactionRemoved += async (Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction) =>
            {
                var socketGuild = (channel as SocketTextChannel)?.Guild;
                var guild = await Guilds.GetAsync(socketGuild);
                if (guild is null) return;

                if (guild.Admin.Rulebox.Enabled)
                {
                    await Rulebox.OnReactionRemoved(cache, channel, reaction);
                }
            };

            socketClient.MessageReceived += async (SocketMessage message) =>
            {
                try
                {
                    await AutoModeration.ValidateMessage(message);
                    await services.GetRequiredService<CommandHandler>().HandleCommandAsync(message);

                }
                catch (Exception ex)
                {
                    await Debug.LogCriticalAsync("Core", ex.Message, ex);
                }
            };

            socketClient.MessageUpdated += async (Cacheable<IMessage, ulong> before, SocketMessage message, ISocketMessageChannel channel) =>
            {
                var guild = await Guilds.GetAsync((message.Author as SocketGuildUser).Guild);
                if (guild is null) return;

                if (guild.Moderation.StaffLogs.Enabled)
                {
                    await AutoModeration.ValidateMessage(message);
                }
            };

            socketClient.MessageDeleted += async (Cacheable<IMessage, ulong> message, ISocketMessageChannel channel) =>
            {
                var guild = await Guilds.GetAsync((message.Value.Author as SocketGuildUser).Guild);
                if (guild is null) return;

                if (guild.Moderation.StaffLogs.Enabled)
                {
                    await StaffLogs.LogMessageDeletion(message, channel);
                }
            };
        }

        private void HookUserEvents()
        {
            socketClient.UserJoined += async (SocketGuildUser socketGuildUser) =>
            {
                var guild = await Guilds.GetAsync(socketGuildUser.Guild);
                if (guild is null) return;

                if (guild.General.Announce.Enabled)
                {
                    await Announce.AnnounceUserJoin(socketGuildUser);
                }
            };

            socketClient.UserLeft += async (SocketGuildUser socketGuildUser) =>
            {
                var guild = await Guilds.GetAsync(socketGuildUser.Guild);
                if (guild is null) return;

                if (guild.General.Announce.Enabled)
                {
                    await Announce.AnnounceUserLeft(socketGuildUser);
                }
                if (guild.Admin.Rulebox.Enabled)
                {
                    await Rulebox.RemoveUserReaction(socketGuildUser);
                }
                if (guild.Moderation.StaffLogs.Enabled)
                {
                    await StaffLogs.LogKick(socketGuildUser);
                }
            };

            socketClient.GuildMemberUpdated += async (SocketGuildUser socketGuildUser, SocketGuildUser instigator) =>
            {
                var guild = await Guilds.GetAsync(socketGuildUser.Guild);
                if (guild is null) return;

                // TODO -> NicknameFilter depends on module being enabled
                if (guild.Moderation.Auto.Enabled && guild.Moderation.Auto.NicknameFilter)
                {
                    await AutoModeration.ValidateUsername(guild, socketGuildUser);
                }
            };

            socketClient.UserBanned += async (SocketUser socketUser, SocketGuild socketGuild) =>
            {
                var guild = await Guilds.GetAsync(socketGuild);
                if (guild is null) return;

                if (guild.Moderation.StaffLogs.Enabled)
                {
                    await StaffLogs.LogBan(socketUser, socketGuild);
                }
            };
            socketClient.UserUnbanned += async (SocketUser socketUser, SocketGuild socketGuild) =>
            {
                var guild = await Guilds.GetAsync(socketGuild);
                if (guild is null) return;

                if (guild.Moderation.StaffLogs.Enabled)
                {
                    await StaffLogs.LogUserUnban(socketUser, socketGuild);
                }
            };
        }
    }
}