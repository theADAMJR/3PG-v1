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
            var guild = await Guilds.GetAsync(socketGuild);
            var logChannelId = guild.Moderation.StaffLogs.Channel;
            var logChannel = socketGuild.GetTextChannel(logChannelId);

            var user = await Users.GetAsync(socketUser as SocketGuildUser);
            var ban = user.Status.Bans.LastOrDefault();

            var embed = new EmbedBuilder();
            embed.WithTitle($"Banned");
            embed.AddField("User", socketUser.Mention, true);
            embed.AddField("Reason", ban.Reason, true);
            embed.AddField("Start", ban.Start.ToTimestamp());
            embed.AddField("End", ban.End.ToTimestamp());
            embed.WithColor(Color.DarkPurple);
            
            await logChannel.SendMessageAsync(embed: embed.Build());
        }

        public static async Task LogUserUnban(SocketUser socketUser, SocketGuild socketGuild)
        {
            var socketGuildUser = socketUser as SocketGuildUser;
            if (socketGuildUser is null) return;

            var user = await Users.GetAsync(socketGuildUser);
            var guild = await Guilds.GetAsync(socketGuild);
            var logChannelId = guild.Moderation.StaffLogs.Channel;
            var logChannel = socketGuild.GetTextChannel(logChannelId);

            if (logChannel is null)
            {
                // await socketGuild.DefaultChannel.SendMessageAsync("", embed: await EmbedHandler.CreateBasicEmbed("Staff Logs - User Unbanned", "To use `Staff Logs` please set a **Staff Log Channel** with `/config`", Color.Red));
            }

            var embed = new EmbedBuilder();
            embed.WithTitle($"User Banned");
            embed.AddField("User", socketGuildUser.Mention, true);
            embed.WithColor(Color.DarkPurple);

            await logChannel.SendMessageAsync(embed: embed.Build());
        }

        public static async Task LogKick(SocketGuildUser socketGuildUser)
        {
            var user = await Users.GetAsync(socketGuildUser as SocketGuildUser);

            var kick = user?.Status.Kicks.LastOrDefault();
            if (kick is null) return;

            var socketGuild = socketGuildUser.Guild;
            var guild = await Guilds.GetAsync(socketGuildUser.Guild);
            var logChannelId = guild.Moderation.StaffLogs.Channel;
            var logChannel = socketGuild.GetTextChannel(logChannelId);
            var instigator = socketGuild.GetUser(kick.InstigatorID);

            var embed = new EmbedBuilder();
            embed.WithTitle($"Kicked");
            embed.AddField("User", socketGuildUser.Mention, true);
            embed.AddField("Reason", kick.Reason);
            embed.AddField("By", instigator.Mention ?? "N/A");
            embed.WithColor(Color.DarkPurple);

            await logChannel.SendMessageAsync(embed: embed.Build());
        }

        public static async Task LogMessageDeletion(Cacheable<IMessage, ulong> message, ISocketMessageChannel channel)
        {
            if (!message.HasValue) return;

            var guildAuthor = message.Value.Author as SocketGuildUser;
            if (guildAuthor is null || guildAuthor.IsBot) return;

            var socketGuild = guildAuthor.Guild;
            var guild = await Guilds.GetAsync(socketGuild);
            if (guild.General.RemoveCommandMessages) return;

            var user = await Users.GetAsync(guildAuthor);

            var logChannelId = guild.Moderation.StaffLogs.Channel;
            var logChannel = socketGuild.GetTextChannel(logChannelId);

            if (logChannel is null)
            {
                // await channel.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed("Staff Logs - Message Deleted", "To use `Staff Logs` please set a **Staff Log Channel** with `/config`", Color.Red));
                return;
            }
            var embed = new EmbedBuilder();

            string validation = Auto.GetContentValidation(guild, message.Value.Content.ToString(), user).ToString();
            
            embed.WithTitle("Message Deleted");
            embed.AddField("User", message.Value.Author.Mention, true);
            embed.AddField("Message", $"{message.Value.Content.ToString()}", true);
            embed.AddField("Reason", $"{(string.IsNullOrEmpty(validation) ? "User Removed" : validation.ToSentenceCase())}", true);   
            embed.AddField("Channel", $"{(channel as SocketTextChannel).Mention}", true);
            embed.WithFooter($"Message ID: {message.Value.Id}");
            embed.WithColor(Color.DarkPurple);
            embed.WithCurrentTimestamp();

            await logChannel.SendMessageAsync(embed: embed.Build());
        }
        
        public static async Task LogBulkMessageDeletion(IReadOnlyCollection<Cacheable<IMessage, ulong>> messages, ISocketMessageChannel channel)
        {
            var textChannel = channel as SocketTextChannel;
            var socketGuild = textChannel?.Guild;
            if (socketGuild is null) return;

            var guild = await Guilds.GetAsync(socketGuild);
            var logChannelId = guild.Moderation.StaffLogs.Channel;
            var logChannel = socketGuild.GetTextChannel(logChannelId);

            if (logChannel is null)
            {
                //await channel.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed("Staff Logs - Message Deleted", "To use `Staff Logs` please set a **Staff Log Channel** with `/config`", Color.Red));
                return;
            }
            var embed = new EmbedBuilder();

            embed.WithTitle("Messages Deleted");
            embed.AddField("Count", messages.Count);
            embed.AddField("Channel", $"{textChannel.Mention}", true);
            embed.WithColor(Color.DarkPurple);

            await logChannel.SendMessageAsync(embed: embed.Build());
        }

        public static async Task LogMute(GuildUser user, Punishment punishment)
        {
            var socketGuild = Global.Client.GetGuild(user.GuildID);
            var socketGuildUser = socketGuild.GetUser(user.ID);
            var guild = await Guilds.GetAsync(socketGuildUser.Guild);

            var logChannelId = guild.Moderation.StaffLogs.Channel;
            var logChannel = socketGuildUser.Guild.GetTextChannel(logChannelId);
            if (logChannel is null) return;

            var instigator = socketGuildUser.Guild.GetUser(punishment.InstigatorID);

            var embed = new EmbedBuilder();
            embed.WithTitle($"Mute");
            embed.AddField("User", socketGuildUser.Mention, true);
            embed.AddField("Reason", punishment.Reason);
            embed.AddField("By", instigator.Mention ?? "N/A");
            embed.WithColor(Color.DarkPurple);

            await logChannel.SendMessageAsync(embed: embed.Build());
        }

        public static async Task LogUnmute(GuildUser user, Punishment punishment)
        {
            var socketGuild = Global.Client.GetGuild(user.GuildID);
            var socketGuildUser = socketGuild.GetUser(user.ID);
            var guild = await Guilds.GetAsync(socketGuildUser.Guild);

            var logChannelId = guild.Moderation.StaffLogs.Channel;
            var logChannel = socketGuildUser.Guild.GetTextChannel(logChannelId);
            if (logChannel is null) return;
            
            var instigator = socketGuildUser.Guild.GetUser(punishment?.InstigatorID ?? 0);

            var embed = new EmbedBuilder();
            embed.WithTitle($"Unmute");
            embed.AddField("User", socketGuildUser.Mention);
            embed.AddField("Reason", punishment.Reason);
            embed.AddField("By", instigator?.Mention ?? "N/A");
            embed.WithColor(Color.DarkPurple);
        }

        public static async Task LogWarn(GuildUser user, Punishment punishment)
        {
            var socketGuild = Global.Client.GetGuild(user.GuildID);
            var socketGuildUser = socketGuild.GetUser(user.ID);
            var guild = await Guilds.GetAsync(socketGuildUser.Guild);

            var logChannelId = guild.Moderation.StaffLogs.Channel;
            var logChannel = socketGuildUser.Guild.GetTextChannel(logChannelId);
            if (logChannel is null) return;
            
            var instigator = socketGuildUser.Guild.GetUser(punishment.InstigatorID);

            var embed = new EmbedBuilder();
            embed.WithTitle($"Warn");
            embed.AddField("User", socketGuildUser.Mention, true);
            embed.AddField("Reason", punishment.Reason);
            embed.AddField("By", instigator.Mention ?? "N/A");
            embed.WithColor(Color.DarkPurple);
        }
    }
}