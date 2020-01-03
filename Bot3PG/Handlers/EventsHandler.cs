using System;
using System.Collections.Generic;
using System.Linq;
using Bot3PG.Data;
using Bot3PG.Data.Structs;
using Bot3PG.Modules;
using Bot3PG.Modules.Admin;
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
        private DiscordSocketClient bot;
        private ServiceProvider services;
        private LavaSocketClient lavaClient;

        public EventsHandler(ServiceProvider services, DiscordSocketClient socketClient, LavaSocketClient lavaSocketClient)
        {
            this.bot = socketClient;
            this.services = services;
            this.lavaClient = lavaSocketClient;

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
            lavaClient.Log += async (LogMessage message) => await Debug.LogAsync(message.Source, message.Severity, message.Message);
            services.GetRequiredService<CommandService>().Log += async (LogMessage message) => await Debug.LogAsync(message.Source, message.Severity, message.Message);
            bot.Log += async (LogMessage message) => await Debug.LogAsync(message.Source, message.Severity, message.Message);

        }

        private void HookClientEvents()
        {
            bot.Ready += async () =>
            {
                try
                {
                    await lavaClient.StartAsync(bot);
                    lavaClient.ToggleAutoDisconnect();
                    lavaClient.OnTrackFinished += services.GetService<AudioService>().OnFinished;

                    await bot.SetGameAsync(Global.Config.GameStatus);

                    await new DatabaseManager().UpdateCommands(new CommandHelp());
                }
                catch (Exception ex) { await Debug.LogInformationAsync(ex.Source, ex.Message); }
            };

            bot.JoinedGuild += async (SocketGuild socketGuild) =>
            {
                var newGuild = await Guilds.GetAsync(socketGuild);
                var channel = socketGuild.SystemChannel ?? socketGuild.DefaultChannel;

                var embed = new EmbedBuilder();
                embed.WithTitle($"Hi, I'm {bot.CurrentUser.Username}.");
                embed.AddField("⚙️ Config", $"Customize me to your server's needs at {Global.Config.WebappLink}/servers/{socketGuild.Id}", inline: true);
                embed.AddField("📜 Commands", $"Type {newGuild.General.CommandPrefix}help for a list of commands.", inline: true);
                embed.AddField("❔ Support", $"Need help with {bot.CurrentUser.Username}? Join our Discord for more support: {Global.Config.WebappLink}/support", inline: true);

                await channel.SendMessageAsync(embed: embed.Build());
            };
        }

        private void HookMessageEvents()
        {
            bot.ReactionAdded += async (Cacheable<IUserMessage, ulong> before, ISocketMessageChannel channel, SocketReaction reaction) =>
            {
                var socketGuildUser = reaction.User.Value as SocketGuildUser;
                var guild = await Guilds.GetAsync(socketGuildUser?.Guild);
                if (guild is null) return;

                if (guild.Admin.Rulebox.Enabled)
                {
                    await Rulebox.CheckRuleAgreement(guild, socketGuildUser, reaction);
                }
            };

            bot.ReactionRemoved += async (Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction) =>
            {
                var socketGuild = (channel as SocketTextChannel)?.Guild;
                var guild = await Guilds.GetAsync(socketGuild);
                if (guild is null) return;

                if (guild.Admin.Rulebox.Enabled) await Rulebox.OnReactionRemoved(cache, channel, reaction);
            };

            bot.MessageReceived += async (SocketMessage message) =>
            {
                var textChannel = (message as SocketMessage)?.Channel as SocketTextChannel;
                if (textChannel is null) return;

                var socketGuild = textChannel.Guild;
                var guild = await Guilds.GetAsync(socketGuild);
                if (guild is null) return;

                await AutoModeration.ValidateMessage(message);
                await services.GetRequiredService<CommandHandler>().HandleCommandAsync(message);
            };

            bot.MessageUpdated += async (Cacheable<IMessage, ulong> before, SocketMessage message, ISocketMessageChannel channel) =>
            {
                var socketGuildUser = before.Value?.Author as SocketGuildUser;
                if (socketGuildUser is null) return;

                var guild = await Guilds.GetAsync(socketGuildUser.Guild);
                if (guild is null) return;

                if (guild.Moderation.StaffLogs.Enabled) await AutoModeration.ValidateMessage(message);
            };

            bot.MessagesBulkDeleted += async (IReadOnlyCollection<Cacheable<IMessage, ulong>> messages, ISocketMessageChannel channel) =>
            {
                var socketGuild = (channel as SocketTextChannel)?.Guild;
                if (socketGuild is null) return;
                
                var guild = await Guilds.GetAsync(socketGuild);
                if (guild is null) return;

                bool shouldLog = Array.Exists(guild.Moderation.StaffLogs.LogEvents, l => l == LogEvent.MessageDeleted);
                if (guild.Moderation.StaffLogs.Enabled && shouldLog) await StaffLogs.LogBulkMessageDeletion(messages, channel);
            };

            bot.MessageDeleted += async (Cacheable<IMessage, ulong> message, ISocketMessageChannel channel) =>
            {
                var socketGuild = (message.Value?.Author as SocketGuildUser)?.Guild;
                if (socketGuild is null) return;
                
                var guild = await Guilds.GetAsync(socketGuild);
                if (guild is null) return;

                bool shouldLog = Array.Exists(guild.Moderation.StaffLogs.LogEvents, l => l == LogEvent.MessageDeleted);
                if (guild.Moderation.StaffLogs.Enabled && shouldLog) await StaffLogs.LogMessageDeletion(message, channel);
            };
        }

        private void HookUserEvents()
        {
            bot.UserJoined += async (SocketGuildUser socketGuildUser) =>
            {
                var guild = await Guilds.GetAsync(socketGuildUser.Guild);
                if (guild is null) return;

                if (guild.General.Announce.Enabled && guild.General.Announce.Welcomes) await Announce.AnnounceUserJoin(socketGuildUser);
                if (guild.General.Enabled && guild.General.NewMemberRoles.Length > 0)
                {
                    var socketGuild = socketGuildUser.Guild;
                    var roles = guild.General.NewMemberRoles.Where(id => socketGuild.GetRole(id) != null);
                    var foundRoles = roles.Select(id => socketGuild.GetRole(id));
                    
                    foreach (var role in foundRoles)
                    {
                        try { await socketGuildUser.AddRolesAsync(foundRoles); }
                        catch {}
                    }
                }
            };

            bot.UserLeft += async (SocketGuildUser socketGuildUser) =>
            {
                var guild = await Guilds.GetAsync(socketGuildUser.Guild);
                if (guild is null) return;

                if (guild.General.Announce.Enabled && guild.General.Announce.Goodbyes) await Announce.AnnounceUserLeft(socketGuildUser);
                if (guild.Admin.Rulebox.Enabled) await Rulebox.RemoveUserReaction(socketGuildUser);

                if (ShouldLog(LogEvent.Kick, guild)) await StaffLogs.LogKick(socketGuildUser);
            };

            bot.GuildMemberUpdated += async (SocketGuildUser socketGuildUser, SocketGuildUser instigator) =>
            {
                var guild = await Guilds.GetAsync(socketGuildUser.Guild);
                if (guild is null) return;

                if (guild.Moderation.Auto.Enabled && guild.Moderation.Auto.NicknameFilter) await AutoModeration.ValidateUsername(guild, socketGuildUser);
            };

            bot.UserBanned += async (SocketUser socketUser, SocketGuild socketGuild) =>
            {
                var guild = await Guilds.GetAsync(socketGuild);
                if (guild is null) return;

                if (ShouldLog(LogEvent.Ban, guild)) await StaffLogs.LogBan(socketUser, socketGuild);
            };
            bot.UserUnbanned += async (SocketUser socketUser, SocketGuild socketGuild) =>
            {
                var guild = await Guilds.GetAsync(socketGuild);
                if (guild is null) return;

                if (ShouldLog(LogEvent.Unban, guild)) await StaffLogs.LogUserUnban(socketUser, socketGuild);
            };

            GuildUser.Muted += async (GuildUser guildUser, Punishment punishment) =>
            {
                var socketGuild = bot.GetGuild(guildUser.GuildID);
                var guild = await Guilds.GetAsync(socketGuild);
                if (guild is null) return;

                if (ShouldLog(LogEvent.Mute, guild)) await StaffLogs.LogMute(guildUser, punishment);
            };
            GuildUser.Unmuted += async (GuildUser guildUser, Punishment punishment) =>
            {
                var socketGuild = bot.GetGuild(guildUser.GuildID);
                var guild = await Guilds.GetAsync(socketGuild);
                if (guild is null) return;

                if (ShouldLog(LogEvent.Unmute, guild)) await StaffLogs.LogUnmute(guildUser, punishment);
            };
            GuildUser.Warned += async (GuildUser guildUser, Punishment punishment) =>
            {
                var socketGuild = bot.GetGuild(guildUser.GuildID);
                var guild = await Guilds.GetAsync(socketGuild);
                if (guild is null) return;

                if (ShouldLog(LogEvent.Warn, guild)) await StaffLogs.LogWarn(guildUser, punishment);
            };
        }

        private bool ShouldLog(LogEvent logEvent, Guild guild) 
        {
            var hasFilter = Array.Exists(guild.Moderation.StaffLogs.LogEvents, l => l == logEvent);
            return guild.Moderation.StaffLogs.IsAllowed(hasFilter);
        }
    }
}