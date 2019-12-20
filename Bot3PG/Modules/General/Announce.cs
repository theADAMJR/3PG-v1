using Bot3PG.Data;
using Bot3PG.Utils;
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
            if (socketGuildUser.IsBot) return;
            var guild = await Guilds.GetAsync(socketGuildUser.Guild);

            var random = new Random();
            var announce = guild.General.Announce;

            int randomIndex = random.Next(0, announce.WelcomeMessages.Length);

            var embed = new EmbedBuilder();

            string initializedMessage = CommandUtils.SetGuildVariables(announce.WelcomeMessages[randomIndex], socketGuildUser);
            embed.AddField($"**Welcome**", initializedMessage);
            embed.WithColor(Color.DarkGreen);

            var socketGuild = socketGuildUser.Guild;
            var channel = socketGuild.GetTextChannel(announce.Channel) ?? socketGuild.SystemChannel ?? socketGuild.DefaultChannel;
            if (channel != null)
            {
                await channel.SendMessageAsync("", embed: embed.Build());
            }
        }

        public static async Task AnnounceUserLeft(SocketGuildUser socketGuildUser)
        {
            var user = await Users.GetAsync(socketGuildUser);
            if (socketGuildUser as SocketUser == Global.Client.CurrentUser || user.Status.IsBanned) return;

            var guild = await Guilds.GetAsync(socketGuildUser.Guild);
            var announce = guild.General.Announce;

            var random = new Random();
            int randomIndex = random.Next(0, announce.GoodbyeMessages.Length);

            var embed = new EmbedBuilder();

            string initializedMessage = CommandUtils.SetGuildVariables(announce.GoodbyeMessages[randomIndex], socketGuildUser);
            embed.AddField($"**Goodbye**", initializedMessage);            
            embed.WithColor(Color.DarkRed);

            var socketGuild = socketGuildUser.Guild;
            var channel = socketGuild.GetTextChannel(announce.Channel) ?? socketGuild.SystemChannel ?? socketGuild.DefaultChannel;
            if (channel != null)
            {
                await channel.SendMessageAsync("", embed: embed.Build());
            }
        }
    }
}