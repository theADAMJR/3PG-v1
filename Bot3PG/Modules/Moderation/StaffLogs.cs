using Bot3PG.Core;
using Bot3PG.Core.Data;
using Bot3PG.Handlers;
using Bot3PG.Moderation;
using Bot3PG.Utilities;
using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace Bot3PG.Modules.Moderation
{
    public class StaffLogs
    {
        public async Task LogBan(SocketUser socketUser, SocketGuild socketGuild)
        {
            var guild = await Guilds.GetAsync(socketGuild);
            var logChannel = guild.Moderation.StaffLogs.Channel;

            var user = await Users.GetAsync(socketUser as SocketGuildUser);
            var ban = user.Status[PunishmentType.Ban];
            Users.ResetAsync(socketUser as SocketGuildUser); // TODO - config

            var embed = new EmbedBuilder();
            embed.WithTitle($"User Banned");
            embed.AddField("User", socketUser.Mention, true);
            embed.AddField("Reason", ban.Reason, true);
            embed.AddField("Start", ban.Start.ToTimestamp());
            embed.AddField("End", ban.End.ToTimestamp());
            embed.WithColor(Color.DarkPurple);
            await logChannel.SendMessageAsync(embed: embed.Build());
        }

        public async Task OnUserUnbanned(SocketUser socketUser, SocketGuild socketGuild)
        {
            var socketGuildUser = socketUser as SocketGuildUser;
            if (socketGuildUser is null) return;

            var user = await Users.GetAsync(socketGuildUser);
            var guild = await Guilds.GetAsync(socketGuild);
            var logChannel = guild.Moderation.StaffLogs.Channel;
            if (logChannel is null)
            {
                await socketGuild.DefaultChannel.SendMessageAsync("", embed: await EmbedHandler.CreateBasicEmbed("Staff Logs - User Unbanned", "To use `Staff Logs` please set a **Staff Log Channel** with `/config`", Color.Red));
            }

            var embed = new EmbedBuilder();
            embed.WithTitle($"User Banned");
            embed.AddField("User", socketGuildUser.Mention, true);
            embed.WithColor(Color.DarkPurple);

            await logChannel.SendMessageAsync(embed: embed.Build());
        }

        public async Task LogKick(SocketGuildUser socketGuildUser)
        {
            var user = await Users.GetAsync(socketGuildUser as SocketGuildUser);
            var kick = user?.Status[PunishmentType.Kick];

            if (kick is null) return;

            var guild = await Guilds.GetAsync(socketGuildUser.Guild);
            var logChannel = guild.Moderation.StaffLogs.Channel;


            var embed = new EmbedBuilder();
            embed.WithTitle($"User Kicked");
            embed.AddField("User", socketGuildUser.Mention, true);
            embed.AddField("Kick Reason", kick.Reason);
            embed.WithColor(Color.DarkPurple);

            await logChannel.SendMessageAsync(embed: embed.Build());
        }

        public async Task LogMessageDeletion(Cacheable<IMessage, ulong> msg, ISocketMessageChannel channel)
        {
            if (!msg.HasValue) return;

            var guildAuthor = msg.Value.Author as SocketGuildUser;
            if (guildAuthor is null || guildAuthor.IsBot) return;

            var socketGuild = guildAuthor.Guild;
            var guild = await Guilds.GetAsync(socketGuild);
            var logChannel = guild.Moderation.StaffLogs.Channel;
            if (logChannel is null)
            {
                await channel.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed("Staff Logs - Message Deleted", "To use `Staff Logs` please set a **Staff Log Channel** with `/config`", Color.Red));
            }
            var embed = new EmbedBuilder();
            embed.WithTitle("Message Deleted");
            embed.AddField("User", msg.Value.Author.Mention, true);
            embed.AddField("Channel", $"{(channel as SocketTextChannel).Mention}", true);
            embed.AddField("Auto Deletion", $"{!AutoModeration.IsContentValid(guild, msg.Value.Content.ToString())}", true);
            embed.AddField("Message", $"{msg.Value.Content.ToString()}", true);
            embed.WithFooter($"Message ID: {msg.Value.Id}");
            embed.WithCurrentTimestamp();
            embed.WithColor(Color.DarkPurple);

            await logChannel.SendMessageAsync(embed: embed.Build());
        }
    }
}