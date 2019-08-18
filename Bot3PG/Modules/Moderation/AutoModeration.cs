using Bot3PG.Core.Data;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace Bot3PG.Modules.Moderation
{
    public class AutoModeration
    {
        public async Task OnMessageRecieved(SocketMessage msg)
        {
            var guildAuthor = msg.Author as SocketGuildUser;
            if (guildAuthor is null || guildAuthor.IsBot) return;

            var user = await Users.GetAsync(guildAuthor);

            bool validMessage = IsMessageValid(msg.Content);
            if (!validMessage)
            {
                await AutoPunishUser(guildAuthor, "Explicit message");
                await msg.DeleteAsync();
                if (msg.Author != null)
                {
                    user.XP.ExtendXPCooldown();
                }
            }
        }
        public static bool IsMessageValid(string msgContents)
        {
            // TODO - append guildconfig ban words
            var banWords = BannedWords.GetWords();
            var banLinks = BannedLinks.GetLinks();
            var lowerCaseMsg = msgContents.ToLower();

            foreach (var banWord in banWords)
            {
                if (lowerCaseMsg.Contains(banWord.ToLower()))
                {
                    return false;
                }
            }
            foreach (var banLink in banLinks)
            {
                if (lowerCaseMsg.Contains(banLink.ToLower()))
                {
                    return false;
                }
            }
            return true;
        }

        public static async Task AutoPunishUser(SocketGuildUser socketGuildUser, string reason)
        {
            var guild = await Guilds.GetAsync(socketGuildUser.Guild);

            if (socketGuildUser.GuildPermissions.Administrator) return;

            var user = await Users.GetAsync(socketGuildUser);

            switch (user.Status.WarningsCount)
            {
                case int warnings when (warnings >= guild.Config.WarningsForBan && guild.Config.WarningsForBan > 0):
                    await user.BanAsync(TimeSpan.FromDays(-1), reason);
                    break;
                case int warnings when (warnings >= guild.Config.WarningsForKick && guild.Config.WarningsForKick > 0):
                    await user.KickAsync(reason);
                    break;
                default:
                    await user.WarnAsync(reason);
                    break;
            }
        }
    }
}