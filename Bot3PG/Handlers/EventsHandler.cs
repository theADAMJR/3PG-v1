using System;
using System.Collections.Generic;
using Bot3PG.Data;
using Bot3PG.Data.Structs;
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

                if (guild.Admin.Rulebox.Enabled)
                {
                    await Rulebox.CheckRuleAgreement(guild, socketGuildUser, reaction);
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
                var textChannel = (message as SocketMessage)?.Channel as SocketTextChannel;
                if (textChannel is null) return;

                var socketGuild = textChannel.Guild;
                var guild = await Guilds.GetAsync(socketGuild);
                if (guild is null) return;

                await AutoModeration.ValidateMessage(message);
                await services.GetRequiredService<CommandHandler>().HandleCommandAsync(message);
            };

            socketClient.MessageUpdated += async (Cacheable<IMessage, ulong> before, SocketMessage message, ISocketMessageChannel channel) =>
            {
                var socketGuildUser = before.Value?.Author as SocketGuildUser;
                if (socketGuildUser is null) return;

                var guild = await Guilds.GetAsync(socketGuildUser.Guild);
                if (guild is null) return;

                if (guild.Moderation.StaffLogs.Enabled)
                {
                    await AutoModeration.ValidateMessage(message);
                }
            };

            socketClient.MessagesBulkDeleted += async (IReadOnlyCollection<Cacheable<IMessage, ulong>> messages, ISocketMessageChannel channel) =>
            {
                var socketGuild = (channel as SocketTextChannel)?.Guild;
                if (socketGuild is null) return;
                
                var guild = await Guilds.GetAsync(socketGuild);
                if (guild is null) return;

                bool shouldLog = Array.Exists(guild.Moderation.StaffLogs.LogEvents, l => l == LogEvent.MessageDeleted);
                if (guild.Moderation.StaffLogs.Enabled && shouldLog)
                {
                    await StaffLogs.LogBulkMessageDeletion(messages, channel);
                }
            };

            socketClient.MessageDeleted += async (Cacheable<IMessage, ulong> message, ISocketMessageChannel channel) =>
            {
                var socketGuild = (message.Value?.Author as SocketGuildUser)?.Guild;
                if (socketGuild is null) return;
                
                var guild = await Guilds.GetAsync(socketGuild);
                if (guild is null) return;

                bool shouldLog = Array.Exists(guild.Moderation.StaffLogs.LogEvents, l => l == LogEvent.MessageDeleted);
                if (guild.Moderation.StaffLogs.Enabled && shouldLog)
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

                if (guild.General.Announce.Enabled && guild.General.Announce.Welcomes)
                {
                    await Announce.AnnounceUserJoin(socketGuildUser);
                }
            };

            socketClient.UserLeft += async (SocketGuildUser socketGuildUser) =>
            {
                var guild = await Guilds.GetAsync(socketGuildUser.Guild);
                if (guild is null) return;

                if (guild.General.Announce.Enabled && guild.General.Announce.Goodbyes)
                {
                    await Announce.AnnounceUserLeft(socketGuildUser);
                }
                if (guild.Admin.Rulebox.Enabled)
                {
                    await Rulebox.RemoveUserReaction(socketGuildUser);
                }
                bool shouldLog = Array.Exists(guild.Moderation.StaffLogs.LogEvents, l => l == LogEvent.Kick);
                if (guild.Moderation.StaffLogs.Enabled && shouldLog)
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

                bool shouldLog = Array.Exists(guild.Moderation.StaffLogs.LogEvents, l => l == LogEvent.Ban);
                if (guild.Moderation.StaffLogs.Enabled && shouldLog)
                {
                    await StaffLogs.LogBan(socketUser, socketGuild);
                }
            };
            socketClient.UserUnbanned += async (SocketUser socketUser, SocketGuild socketGuild) =>
            {
                var guild = await Guilds.GetAsync(socketGuild);
                if (guild is null) return;

                bool shouldLog = Array.Exists(guild.Moderation.StaffLogs.LogEvents, l => l == LogEvent.Unban);
                if (guild.Moderation.StaffLogs.Enabled && shouldLog)
                {
                    await StaffLogs.LogUserUnban(socketUser, socketGuild);
                }
            };

            GuildUser.Muted += async (GuildUser guildUser, Punishment punishment) =>
            {
                var socketGuild = socketClient.GetGuild(guildUser.GuildID);
                var guild = await Guilds.GetAsync(socketGuild);
                if (guild is null) return;

                bool shouldLog = Array.Exists(guild.Moderation.StaffLogs.LogEvents, l => l == LogEvent.Mute);
                if (guild.Moderation.StaffLogs.Enabled && shouldLog)
                {
                    await StaffLogs.LogMute(guildUser, punishment);
                }
            };
            GuildUser.Unmuted += async (GuildUser guildUser, Punishment punishment) =>
            {
                var socketGuild = socketClient.GetGuild(guildUser.GuildID);
                var guild = await Guilds.GetAsync(socketGuild);
                if (guild is null) return;

                bool shouldLog = Array.Exists(guild.Moderation.StaffLogs.LogEvents, l => l == LogEvent.Mute);
                if (guild.Moderation.StaffLogs.Enabled && shouldLog)
                {
                    await StaffLogs.LogUnmute(guildUser, punishment);
                }
            };
            GuildUser.Warned += async (GuildUser guildUser, Punishment punishment) =>
            {
                var socketGuild = socketClient.GetGuild(guildUser.GuildID);
                var guild = await Guilds.GetAsync(socketGuild);
                if (guild is null) return;

                bool shouldLog = Array.Exists(guild.Moderation.StaffLogs.LogEvents, l => l == LogEvent.Mute);
                if (guild.Moderation.StaffLogs.Enabled && shouldLog)
                {
                    await StaffLogs.LogWarn(guildUser, punishment);
                }
            };
        }
    }
}