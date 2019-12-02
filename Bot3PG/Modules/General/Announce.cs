using Bot3PG.Core.Data;
using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace Bot3PG.Modules.General
{
    public static class Announce
    {
        public static async Task AnnounceUserJoin(SocketGuildUser socketGuildUser)
        {
            if (!socketGuildUser.IsBot) return;
            var guild = await Guilds.GetAsync(socketGuildUser.Guild);

            var random = new Random();
            var announceConfig = guild.General.Announce;

            int randomIndex = random.Next(0, announceConfig.WelcomeMessages.Count);

            var embed = new EmbedBuilder();
            embed.AddField($"**Welcome!**", announceConfig.WelcomeMessages[randomIndex]);
            embed.WithColor(Color.DarkGreen);

            await announceConfig.Channel.SendMessageAsync("", embed: embed.Build());
        }

        public static async Task AnnounceUserLeft(SocketGuildUser socketGuildUser)
        {
            var user = await Users.GetAsync(socketGuildUser);
            if (socketGuildUser as SocketUser == Global.Client.CurrentUser || user.Status.IsBanned) return;

            var guild = await Guilds.GetAsync(socketGuildUser.Guild);
            var announceConfig = guild.General.Announce;

            var random = new Random();
            int randomIndex = random.Next(0, announceConfig.GoodbyeMessages.Count);

            var embed = new EmbedBuilder();
            embed.AddField($"**Goodbye!**", announceConfig.GoodbyeMessages[randomIndex]);
            embed.WithColor(Color.DarkRed);

            await announceConfig.Channel.SendMessageAsync("", embed: embed.Build());
        }
    }
}