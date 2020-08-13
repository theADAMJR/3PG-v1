using Bot3PG.Data;
using Bot3PG.Data.Structs;
using Bot3PG.Handlers;
using Bot3PG.Utils;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bot3PG.Modules.Moderation
{
    public static class StaffLogs
    {
        private class StaffLog
        {
            public SocketTextChannel Channel { get; set; }
            public string Colour { get; set; }
        }

        public static async Task LogBan(SocketUser socketUser, SocketGuild socketGuild, Punishment ban = null)
        {
            try
            {
                if (socketUser is null) return;

                var user = await Users.GetAsync(socketUser as SocketGuildUser);
                var guild = await Guilds.GetAsync(socketGuild);
                var log = ValidateLog(guild, socketGuild, LogEvent.Ban);

                ban ??= user.Status.Bans.LastOrDefault();
                var embed = CreateTimestampPunishmentEmbed(socketUser, ban, log);

                await log.Channel.SendMessageAsync(embed: embed.Build());
            }
            catch (Exception) {}
        }

        public static async Task LogUnban(SocketUser socketUser, SocketGuild socketGuild)
        {
            try
            {
                var discordUser = socketUser as SocketGuildUser;
                if (discordUser is null) return;

                var user = await Users.GetAsync(discordUser);
                var guild = await Guilds.GetAsync(socketGuild);
                var log = ValidateLog(guild, socketGuild, LogEvent.Unban);
                var embed = CreateUnbanEmbed(discordUser, log);

                await log.Channel.SendMessageAsync(embed: embed.Build());
            }
            catch (Exception) {}
        }

        public static async Task LogKick(SocketGuildUser discordUser, Punishment kick = null)
        {
            try
            {
                var user = await Users.GetAsync(discordUser as SocketGuildUser);
                kick ??= user?.Status.Kicks.LastOrDefault();
                if (kick is null) return;

                var socketGuild = discordUser.Guild;
                var guild = await Guilds.GetAsync(discordUser.Guild);
                var log = ValidateLog(guild, socketGuild, LogEvent.Kick);
                var embed = CreatePunishmentEmbed(kick, discordUser, log.Colour, LogEvent.Kick);

                await log.Channel.SendMessageAsync(embed: embed);
            }
            catch (Exception) {}
        }

        public static async Task LogMessageDeletion(Cacheable<IMessage, ulong> message, ISocketMessageChannel channel)
        {
            try
            {
                if (!message.HasValue) return;

                var guildAuthor = message.Value.Author as SocketGuildUser;
                if (guildAuthor is null || guildAuthor.IsBot) return;

                var socketGuild = guildAuthor.Guild;
                var guild = await Guilds.GetAsync(socketGuild);
                
                string prefix = guild.General.CommandPrefix;
                bool isCommand = message.Value.Content.Substring(0, prefix.Length).Contains(prefix);
                if (guild.General.RemoveCommandMessages && isCommand || isCommand) return;

                var user = await Users.GetAsync(guildAuthor);
                var log = ValidateLog(guild, socketGuild, LogEvent.MessageDeleted);

                string validation = Auto.GetContentValidation(guild, message.Value.Content.ToString(), user).ToString();
                string reason = string.IsNullOrEmpty(validation) ? "User Removed" : validation.ToSentenceCase();
                var embed = CreateMessageDeletedEmbed(message, channel, reason, log.Colour);

                await log.Channel.SendMessageAsync(embed: embed);
            }
            catch (Exception) {}
        }

        public static async Task LogBulkMessageDeletion(IReadOnlyCollection<Cacheable<IMessage, ulong>> messages, ISocketMessageChannel channel)
        {
            try
            {
                var textChannel = channel as SocketTextChannel;
                var socketGuild = textChannel?.Guild;
                if (socketGuild is null) return;

                var guild = await Guilds.GetAsync(socketGuild);
                var log = ValidateLog(guild, socketGuild, LogEvent.MessageBulkDeleted);
                var embed = CreateBulkDeletedEmbed(messages, textChannel, log);

                await log.Channel.SendMessageAsync(embed: embed.Build());
            }
            catch (Exception) {}
        }
        public static async Task LogMute(GuildUser user, Punishment punishment)
        {
            try
            {
                var socketGuild = Global.Client.GetGuild(user.GuildID);
                var discordUser = socketGuild.GetUser(user.ID);
                var guild = await Guilds.GetAsync(discordUser.Guild);

                var log = ValidateLog(guild, socketGuild, LogEvent.Mute);
                var embed = CreatePunishmentEmbed(punishment, discordUser, log.Colour, LogEvent.Mute);

                await log.Channel.SendMessageAsync(embed: embed);
            }
            catch (Exception) {}
        }

        public static async Task LogUnmute(GuildUser user, Punishment mute = null)
        {
            try
            {
                var socketGuild = Global.Client.GetGuild(user.GuildID);
                var discordUser = socketGuild.GetUser(user.ID);
                var guild = await Guilds.GetAsync(discordUser.Guild);

                mute ??= user.Status.Mutes.LastOrDefault();

                var log = ValidateLog(guild, socketGuild, LogEvent.Unmute);
                var instigator = discordUser.Guild.GetUser(mute?.InstigatorID ?? 0);
                var embed = CreatePunishmentEmbed(mute, discordUser, log.Colour, LogEvent.Unmute);
                
                await log.Channel.SendMessageAsync(embed: embed);
            }
            catch (Exception) {}
        }

        public static async Task LogWarn(GuildUser user, Punishment warn)
        {
            try
            {
                var socketGuild = Global.Client.GetGuild(user.GuildID);
                var discordUser = socketGuild.GetUser(user.ID);
                var guild = await Guilds.GetAsync(discordUser.Guild);

                var log = ValidateLog(guild, socketGuild, LogEvent.Warn);
                var embed = CreatePunishmentEmbed(warn, discordUser, log.Colour, LogEvent.Warn);

                await log.Channel.SendMessageAsync(embed: embed);
            }
            catch (Exception) {}
        }

        private static StaffLog ValidateLog(Guild guild, SocketGuild socketGuild, LogEvent logEvent)
        {           
            var log = guild.Moderation.StaffLogs.LogEvents.First(l => l.LogEvent == logEvent);
            var logChannel = socketGuild.GetTextChannel(log.Channel);
            if (logChannel is null) 
                throw new InvalidOperationException("Log channel cannot be null.");

            return new StaffLog { Channel = logChannel, Colour = log.Colour };
        }

        private static Embed CreatePunishmentEmbed(Punishment punishment, SocketGuildUser discordUser, string colour, LogEvent logEvent)
        {
            var instigator = discordUser.Guild.GetUser(punishment.InstigatorID);

            return new EmbedBuilder()
                .WithTitle(logEvent.ToString().ToSentenceCase())
                .AddField("User", discordUser.Mention, inline: true)
                .AddField("Reason", punishment.Reason, inline: true)
                .AddField("By", instigator.Mention ?? "N/A", inline: true)
                .WithColor(StringToColor(colour))
                .Build();
        }

        private static EmbedBuilder CreateTimestampPunishmentEmbed(SocketUser socketUser, Punishment punishment, StaffLog log)
        {
            var instigator = log.Channel.Guild.GetUser(punishment.InstigatorID);
            
            return new EmbedBuilder()
                .WithTitle(punishment.Type.ToString().ToSentenceCase())
                .AddField("User", socketUser.Mention, inline: true)
                .AddField("Reason", punishment.Reason, inline: true)
                .AddField("By", instigator.Mention, inline: true)
                .AddField("Start", punishment.Start.ToTimestamp())
                .AddField("End", punishment.End.ToTimestamp(), inline: true)
                .WithColor(StringToColor(log.Colour));
        }

        private static Color StringToColor(string colour)
        {
            var colourObject = (System.Drawing.Color)new System.Drawing.ColorConverter().ConvertFromString(colour.ToUpper());
            return new Color(colourObject.R, colourObject.G, colourObject.B);
        }

        private static Embed CreateMessageDeletedEmbed(Cacheable<IMessage, ulong> message, ISocketMessageChannel channel, string reason, string colour)
        {
            return new EmbedBuilder()
                .WithTitle("Message Deleted")
                .AddField("User", message.Value.Author.Mention, inline: true)
                .AddField("Message", $"{message.Value.Content}", inline: true)
                .AddField("Reason", $"{reason}", inline: true)
                .AddField("Channel", $"{(channel as SocketTextChannel).Mention}")
                .WithFooter($"Message ID: {message.Value.Id}")
                .WithColor(StringToColor(colour))
                .WithCurrentTimestamp()
                .Build();
        }

        private static EmbedBuilder CreateBulkDeletedEmbed(IReadOnlyCollection<Cacheable<IMessage, ulong>> messages, SocketTextChannel textChannel, StaffLog log)
        {
            return new EmbedBuilder()
                .WithTitle("Messages Deleted")
                .AddField("Count", messages.Count)
                .AddField("Channel", $"{textChannel.Mention}", true)
                .WithColor(StringToColor(log.Colour));
        }

        private static EmbedBuilder CreateUnbanEmbed(SocketGuildUser discordUser, StaffLog log)
        {
            return new EmbedBuilder()
                .WithTitle($"Unban")
                .AddField("User", discordUser.Mention, true)
                .WithColor(StringToColor(log.Colour));
        }
    }
}