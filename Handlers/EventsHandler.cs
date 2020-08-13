using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
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

        private void UpdateBotStats()
        {   
            new Task(async() =>
            {
                using var client = new HttpClient();
                var startup = DateTime.Now - Global.Uptime;
                var values = new Dictionary<string, string>
                {
                    { "latency", bot.Latency.ToString() }, 
                    { "startup", startup.ToString() }
                };
                var body = new FormUrlEncodedContent(values);
                try 
                {
                    // TODO: make dynamic
                    await client.PostAsync($"http://3pg.xyz/api/bot-stats?token=123", body);
                    await Debug.LogAsync("events", LogSeverity.Verbose, "Updated bot stats");
                }
                catch { await Debug.LogErrorAsync("events", "Failed to update bot stats"); }
                await Task.Delay(60 * 1000);

                UpdateBotStats();
            }).Start();
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
                    lavaClient.OnTrackFinished += services.GetService<AudioService>().OnFinished;

                    await bot.SetGameAsync(Global.Config.Status);

                    // UpdateBotStats();
                    await new DatabaseManager(Global.DatabaseConfig).UpdateCommands(new CommandHelp());
                }
                catch (Exception ex) { await Debug.LogCriticalAsync(ex.Source, ex.Message); }
            };

            bot.JoinedGuild += async (SocketGuild socketGuild) =>
            {
                var newGuild = await Guilds.GetAsync(socketGuild);
                var channel = socketGuild.SystemChannel ?? socketGuild.DefaultChannel;

                var embed = new EmbedBuilder();
                embed.WithTitle($"Hi, I'm {bot.CurrentUser.Username}!");
                embed.AddField("⚙️ Config", $"Customize me to your server's needs at {Global.Config.DashboardURL}/servers/{socketGuild.Id}", inline: true);
                embed.AddField("📜 Commands", $"Type {newGuild.General.CommandPrefix}help for a list of commands.", inline: true);
                embed.AddField("❔ Support", $"Need help with {bot.CurrentUser.Username}? Join our Discord for more support: {Global.Config.DashboardURL}/support", inline: true);

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

                if (guild.Admin.Rulebox.Enabled) await Rulebox.CheckRuleAgreement(guild, socketGuildUser, reaction);

                if (reaction.Message.IsSpecified && reaction.Message.Value != null)
                    await Users.CheckReputationAdded(reaction.Message.Value, reaction);
            };

            bot.ReactionRemoved += async (Cacheable<IUserMessage, ulong> before, ISocketMessageChannel channel, SocketReaction reaction) =>
            {
                var socketGuild = (channel as SocketTextChannel)?.Guild;
                var guild = await Guilds.GetAsync(socketGuild);
                if (guild is null) return;

                if (guild.Admin.Rulebox.Enabled) await Rulebox.OnReactionRemoved(before, channel, reaction);

                if (reaction.Message.IsSpecified && reaction.Message.Value != null)
                    await Users.CheckReputationRemoved(reaction.Message.Value, reaction);
            };

            bot.MessageReceived += async (SocketMessage message) =>
            {
                var textChannel = (message as SocketMessage)?.Channel as SocketTextChannel;
                if (textChannel is null) return;

                var socketGuild = textChannel.Guild;
                var guild = await Guilds.GetAsync(socketGuild);
                if (guild is null) return;

                await Auto.ValidateMessage(message);
                await services.GetRequiredService<CommandHandler>().HandleCommandAsync(message);
            };

            bot.MessageUpdated += async (Cacheable<IMessage, ulong> before, SocketMessage message, ISocketMessageChannel channel) =>
            {
                var socketGuildUser = before.Value?.Author as SocketGuildUser;
                if (socketGuildUser is null) return;

                var guild = await Guilds.GetAsync(socketGuildUser.Guild);
                if (guild is null) return;

                if (guild.Moderation.StaffLogs.Enabled) await Auto.ValidateMessage(message);
            };

            bot.MessagesBulkDeleted += async (IReadOnlyCollection<Cacheable<IMessage, ulong>> messages, ISocketMessageChannel channel) =>
            {
                var socketGuild = (channel as SocketTextChannel)?.Guild;
                if (socketGuild is null) return;
                
                var guild = await Guilds.GetAsync(socketGuild);
                if (guild is null) return;

                if (guild.Moderation.StaffLogs.Enabled && ShouldLog(LogEvent.MessageBulkDeleted, guild)) 
                    await StaffLogs.LogBulkMessageDeletion(messages, channel);
            };

            bot.MessageDeleted += async (Cacheable<IMessage, ulong> message, ISocketMessageChannel channel) =>
            {
                var socketGuild = (message.Value?.Author as SocketGuildUser)?.Guild;
                if (socketGuild is null) return;
                
                var guild = await Guilds.GetAsync(socketGuild);
                if (guild is null) return;

                if (guild.Moderation.StaffLogs.Enabled && ShouldLog(LogEvent.MessageDeleted, guild)) 
                    await StaffLogs.LogMessageDeletion(message, channel);
            };
        }

        private void HookUserEvents()
        {
            bot.UserJoined += async (SocketGuildUser socketGuildUser) =>
            {
                var guild = await Guilds.GetAsync(socketGuildUser.Guild);
                if (guild is null) return;

                if (guild.General.Announce.IsAllowed(guild.General.Announce.Welcomes.Enabled))
                    await Announce.AnnounceUserJoin(socketGuildUser);
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

                if (guild.General.Announce.IsAllowed(guild.General.Announce.Goodbyes.Enabled)) 
                    await Announce.AnnounceUserLeft(socketGuildUser);
                if (guild.Admin.Rulebox.Enabled) 
                    await Rulebox.RemoveUserReaction(socketGuildUser);

                if (ShouldLog(LogEvent.Kick, guild)) 
                    await StaffLogs.LogKick(socketGuildUser);
            };

            bot.GuildMemberUpdated += async (SocketGuildUser socketGuildUser, SocketGuildUser instigator) =>
            {
                var guild = await Guilds.GetAsync(socketGuildUser.Guild);
                if (guild is null) return;

                if (guild.Moderation.Auto.Enabled && guild.Moderation.Auto.ExplicitUsernamePunishment != PunishmentType.None) 
                    await Auto.ValidateUsername(guild, socketGuildUser);
            };

            bot.UserBanned += async (SocketUser socketUser, SocketGuild socketGuild) =>
            {
                var guild = await Guilds.GetAsync(socketGuild);
                if (guild is null) return;

                if (ShouldLog(LogEvent.Ban, guild)) 
                    await StaffLogs.LogBan(socketUser, socketGuild);
            };
            bot.UserUnbanned += async (SocketUser socketUser, SocketGuild socketGuild) =>
            {
                var guild = await Guilds.GetAsync(socketGuild);
                if (guild is null) return;

                if (ShouldLog(LogEvent.Unban, guild)) 
                    await StaffLogs.LogUnban(socketUser, socketGuild);
            };

            GuildUser.Muted += async (GuildUser guildUser, Punishment punishment) =>
            {
                var socketGuild = bot.GetGuild(guildUser.GuildID);
                var guild = await Guilds.GetAsync(socketGuild);
                if (guild is null) return;

                if (ShouldLog(LogEvent.Mute, guild)) 
                    await StaffLogs.LogMute(guildUser, punishment);
            };
            GuildUser.Unmuted += async (GuildUser guildUser, Punishment punishment) =>
            {
                var socketGuild = bot.GetGuild(guildUser.GuildID);
                var guild = await Guilds.GetAsync(socketGuild);
                if (guild is null) return;

                if (ShouldLog(LogEvent.Unmute, guild)) 
                    await StaffLogs.LogUnmute(guildUser, punishment);
            };
            GuildUser.Warned += async (GuildUser guildUser, Punishment punishment) =>
            {
                var socketGuild = bot.GetGuild(guildUser.GuildID);
                var guild = await Guilds.GetAsync(socketGuild);
                if (guild is null) return;

                if (ShouldLog(LogEvent.Warn, guild))
                    await StaffLogs.LogWarn(guildUser, punishment);
            };
        }

        private bool ShouldLog(LogEvent logEvent, Guild guild) 
        {
            bool hasFilter = guild.Moderation.StaffLogs.LogEvents.FirstOrDefault(l => l.LogEvent == logEvent) != null;
            return guild.Moderation.StaffLogs.IsAllowed(hasFilter);
        }
    }
}