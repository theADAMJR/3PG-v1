using Bot3PG.Core.Users;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Bot3PG.Modules
{
    public class StaffLogs
    {
        public async Task OnUserBanned(SocketUser user, SocketGuild guild)
        {
            var logChannel = Global.Client.GetGuild(guild.Id).GetTextChannel(Global.Config.StaffLogChannelID); // TODO - enable config
            //UserAccounts.ResetUserAccount(user as SocketGuildUser);

            var embed = new EmbedBuilder();
            embed.WithTitle($"User Banned");
            // TODO - add reason
            embed.AddField("User", user.Mention, true);
            embed.WithColor(Color.DarkPurple);

            await logChannel.SendMessageAsync("", embed: embed.Build());
        }

        public async Task OnUserUnbanned(SocketUser user, SocketGuild guild)
        {
            var logChannel = Global.Client.GetGuild(guild.Id).GetTextChannel(Global.Config.StaffLogChannelID); // TODO - enable config
            // TODO - randomized message
            Accounts.ResetUserAccount(user as SocketGuildUser);

            var embed = new EmbedBuilder();
            embed.WithTitle($"User Banned");
            embed.AddField("User", user.Mention, true);
            // TODO - add reason
            embed.WithColor(Color.DarkPurple);

            await logChannel.SendMessageAsync("", embed: embed.Build());
        }

        public async Task OnMessageDeleted(Cacheable<IMessage, ulong> msg, ISocketMessageChannel channel)
        {
            if (!msg.HasValue) return;

            if (msg.Value.Author.IsBot) return;

            var guild = ((SocketGuildChannel)channel).Guild;
            var logChannel = Global.Client.GetGuild(guild.Id).GetTextChannel(Global.Config.StaffLogChannelID); // TODO - guild config

            var embed = new EmbedBuilder();
            embed.WithTitle("Message Deleted");
            embed.AddField("User", msg.Value.Author.Mention, true);
            embed.AddField("Channel", $"{(channel as SocketTextChannel).Mention}", true);
            embed.AddField("Auto Deletion", $"{!AutoModeration.IsMessageValid(msg.Value.Content.ToString())}", true);
            embed.AddField("Message", $"{msg.Value.Content.ToString()}", true);
            embed.WithFooter($"Message ID: {msg.Value.Id}");
            embed.WithCurrentTimestamp();
            embed.WithColor(Color.DarkPurple);

            await logChannel.SendMessageAsync("", embed: embed.Build());
        }
    }
}