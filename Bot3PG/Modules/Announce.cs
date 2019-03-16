using Bot3PG.Core.Users;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Bot3PG.Modules
{
    public class Announce
    {
        public async Task OnUserJoined(SocketGuildUser user)
        {
            if (!Global.Config.AnnounceEnabled) return;
            if (Global.Config.AnnounceChannelID == 0) Global.Config.AnnounceChannelID = user.Guild.DefaultChannel.Id;

            var random = new Random();
            var welcomeChannel = Global.Client.GetGuild(user.Guild.Id).GetTextChannel(Global.Config.AnnounceChannelID);
            string[] welcomeMsg = { $"Welcome {user.Mention}!", $"Welcome to {user.Guild.Name} {user.Mention}!", $"Hello {user.Mention}." };
            int randomIndex = random.Next(0, welcomeMsg.Length);

            var embed = new EmbedBuilder();
            embed.AddField($"**Welcome!**", welcomeMsg[randomIndex]);
            embed.WithColor(Color.DarkGreen);

            await welcomeChannel.SendMessageAsync("", embed: embed.Build());
        }

        public async Task OnUserLeft(SocketGuildUser user)
        {
            if (!Global.Config.AnnounceEnabled) return;
            if (Global.Config.AnnounceChannelID == 0) Global.Config.AnnounceChannelID = user.Guild.DefaultChannel.Id;
            // TODO - if user banned - return

            var random = new Random();
            var goodbyeChannel = Global.Client.GetGuild(user.Guild.Id).GetTextChannel(Global.Config.AnnounceChannelID);
            string[] goodbyeMsg = { $"{user.Mention} accidentally uninstalled System 32.", $"{user.Mention} has left.", $"{user.Mention} rage quit." };
            int randomIndex = random.Next(0, goodbyeMsg.Length);

            // remove reaction
            /*var rulebox = Global.Client.GetGuild(user.Guild.Id).GetTextChannel(Global).GetCachedMessage(Global.MessageIdToTrack);
            _client.GetGuild().GetTextChannel().GetCachedMessage(Global.MessageIdToTrack);
            if (rulebox.??)
            {
                if (rulebox..Emote.Name == "✅" && !user.IsBot)
                {
                    UserAccounts.GetAccount(reaction.User.Value as SocketUser).AgreedToRules = false;
                    UserAccounts.SaveAccounts();

                    var roles = ((SocketGuildUser)reaction.User).Roles.ToList();
                    roles.RemoveAt(0);
                    await user.RemoveRolesAsync(roles);
                }
            }*/

            var embed = new EmbedBuilder();
            embed.AddField($"**Goodbye!**", goodbyeMsg[randomIndex]);
            embed.WithColor(Color.DarkRed);

            await goodbyeChannel.SendMessageAsync("", embed: embed.Build());
        }
    }
}