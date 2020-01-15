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
        public static async Task LogBan(SocketUser socketUser, SocketGuild socketGuild)
        {
            try
            {
                if (socketUser is null) return;

                var user = await Users.GetAsync(socketUser as SocketGuildUser);
                var guild = await Guilds.GetAsync(socketGuild);
                var logChannel = ValidateLogChannel(guild, socketGuild);

                var ban = user.Status.Bans.LastOrDefault();

                var embed = new EmbedBuilder()
                    .WithTitle($"Banned")
                    .AddField("User", socketUser.Mention, true)
                    .AddField("Reason", ban.Reason, true)
                    .AddField("Start", ban.Start.ToTimestamp())
                    .AddField("End", ban.End.ToTimestamp())
                    .WithColor(Color.DarkPurple);
                
                await logChannel.SendMessageAsync(embed: embed.Build());                
            }
            catch (Exception) {}
        }

        public static async Task LogUserUnban(SocketUser socketUser, SocketGuild socketGuild)
        {
            try
            {
                var discordUser = socketUser as SocketGuildUser;
                if (discordUser is null) return;

                var user = await Users.GetAsync(discordUser);
                var guild = await Guilds.GetAsync(socketGuild);
                var logChannel = ValidateLogChannel(guild, socketGuild);

                var embed = new EmbedBuilder()
                    .WithTitle($"User Banned")
                    .AddField("User", discordUser.Mention, true)
                    .WithColor(Color.DarkPurple);

                await logChannel.SendMessageAsync(embed: embed.Build());
            }
            catch (Exception) {}
        }

        public static async Task LogKick(SocketGuildUser discordUser)
        {
            try
            {
                var user = await Users.GetAsync(discordUser as SocketGuildUser);

                var kick = user?.Status.Kicks.LastOrDefault();
                if (kick is null) return;

                var socketGuild = discordUser.Guild;
                var guild = await Guilds.GetAsync(discordUser.Guild);
                var logChannel = ValidateLogChannel(guild, socketGuild);

                var embed = GetPunishmentEmbed(kick, discordUser);
                await logChannel.SendMessageAsync(embed: embed);
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
                if (guild.General.RemoveCommandMessages/* || messageIsACommand*/) return;

                var user = await Users.GetAsync(guildAuthor);
                var logChannel = ValidateLogChannel(guild, socketGuild);

                string validation = Auto.GetContentValidation(guild, message.Value.Content.ToString(), user).ToString();
                string reason = string.IsNullOrEmpty(validation) ? "User Removed" : validation.ToSentenceCase();

                var embed = GetMessageDeletedEmbed(message, channel, reason);
                await logChannel.SendMessageAsync(embed: embed);
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
                var logChannel = ValidateLogChannel(guild, socketGuild);

                var embed = new EmbedBuilder()
                    .WithTitle("Messages Deleted")
                    .AddField("Count", messages.Count)
                    .AddField("Channel", $"{textChannel.Mention}", true)
                    .WithColor(Color.DarkPurple);

                await logChannel.SendMessageAsync(embed: embed.Build());
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

                var logChannel = ValidateLogChannel(guild, socketGuild);

                var embed = GetPunishmentEmbed(punishment, discordUser);
                await logChannel.SendMessageAsync(embed: embed);
            }
            catch (Exception) {}
        }

        public static async Task LogUnmute(GuildUser user, Punishment punishment)
        {
            try
            {
                var socketGuild = Global.Client.GetGuild(user.GuildID);
                var discordUser = socketGuild.GetUser(user.ID);
                var guild = await Guilds.GetAsync(discordUser.Guild);

                var logChannel = ValidateLogChannel(guild, socketGuild);            

                var instigator = discordUser.Guild.GetUser(punishment?.InstigatorID ?? 0);
                var embed = GetPunishmentEmbed(punishment, discordUser);                    
                await logChannel.SendMessageAsync(embed: embed);
            }
            catch (Exception) {}
        }

        public static async Task LogWarn(GuildUser user, Punishment punishment)
        {
            try
            {
                var socketGuild = Global.Client.GetGuild(user.GuildID);
                var discordUser = socketGuild.GetUser(user.ID);
                var guild = await Guilds.GetAsync(discordUser.Guild);

                var logChannel = ValidateLogChannel(guild, socketGuild);
                var embed = GetPunishmentEmbed(punishment, discordUser);
                await logChannel.SendMessageAsync(embed: embed);
            }
            catch (Exception) {}
        }

        private static SocketTextChannel ValidateLogChannel(Guild guild, SocketGuild socketGuild)
        {           
            var logChannelId = guild.Moderation.StaffLogs.Channel;
            var logChannel = socketGuild.GetTextChannel(logChannelId);
            if (logChannel is null) 
                throw new InvalidOperationException("Log channel cannot be null.");

            return logChannel;
        }

        private static Embed GetPunishmentEmbed(Punishment punishment, SocketGuildUser discordUser)
        {
            var instigator = discordUser.Guild.GetUser(punishment.InstigatorID);

            return new EmbedBuilder()
                .WithTitle(punishment.Type.ToString().ToSentenceCase())
                .AddField("User", discordUser.Mention, true)
                .AddField("Reason", punishment.Reason, true)
                .AddField("By", instigator.Mention ?? "N/A")
                .WithColor(Color.DarkPurple)
                .Build();
        }

        private static Embed GetMessageDeletedEmbed(Cacheable<IMessage, ulong> message, ISocketMessageChannel channel, string reason)
        {
            return new EmbedBuilder()
                .WithTitle("Message Deleted")
                .AddField("User", message.Value.Author.Mention, true)
                .AddField("Message", $"{message.Value.Content.ToString()}", true)
                .AddField("Reason", $"{reason}", true)
                .AddField("Channel", $"{(channel as SocketTextChannel).Mention}", true)
                .WithFooter($"Message ID: {message.Value.Id}")
                .WithColor(Color.DarkPurple)
                .WithCurrentTimestamp()
                .Build();
        }
    }
}