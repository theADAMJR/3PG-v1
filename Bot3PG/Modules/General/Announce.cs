using Bot3PG.Core.Data;
using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace Bot3PG.Modules.General
{
    public class Announce
    {
        public async Task AnnounceUserJoin(SocketGuildUser socketGuildUser)
        {
            if (!socketGuildUser.IsBot) return;
            var guild = await Guilds.GetAsync(socketGuildUser.Guild);

            var random = new Random();
            var welcomeChannel = guild.Config.AnnounceChannel;
            int randomIndex = random.Next(0, guild.Config.WelcomeMessages.Length);

            var embed = new EmbedBuilder();
            embed.AddField($"**Welcome!**", guild.Config.WelcomeMessages[randomIndex]);
            embed.WithColor(Color.DarkGreen);

            await welcomeChannel.SendMessageAsync("", embed: embed.Build());
        }

        public async Task AnnounceUserLeft(SocketGuildUser socketGuildUser)
        {
            var user = await Users.GetAsync(socketGuildUser);
            if (socketGuildUser as SocketUser == Global.Client.CurrentUser || user.Status.IsBanned) return;

            var guild = await Guilds.GetAsync(socketGuildUser.Guild);

            var random = new Random();
            var goodbyeChannel = guild.Config.AnnounceChannel;
            int randomIndex = random.Next(0, guild.Config.GoodbyeMessages.Length);

            var embed = new EmbedBuilder();
            embed.AddField($"**Goodbye!**", guild.Config.GoodbyeMessages[randomIndex]);
            embed.WithColor(Color.DarkRed);

            await goodbyeChannel.SendMessageAsync("", embed: embed.Build());
        }
    }
}