using Bot3PG.Core.Data;
using Bot3PG.DataStructs;
using Bot3PG.Handlers;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bot3PG.Modules.Moderation
{
    public class AutoModeration
    {
        public async Task OnMessageRecieved(SocketMessage msg)
        {
            try
            {
                if (!(msg.Author is SocketGuildUser guildAuthor) || guildAuthor.IsBot) return;

                var user = await Users.GetAsync(guildAuthor);
                var guild = await Guilds.GetAsync(guildAuthor.Guild);

                if (!IsMessageValid(guild, guildAuthor.Nickname) || !IsMessageValid(guild, guildAuthor.Username) && guild.Moderation.Auto.NicknameFilter)
                {
                    var dmChannel = await guildAuthor.GetOrCreateDMChannelAsync();
                    await dmChannel.SendMessageAsync(embed: await EmbedHandler.CreateSimpleEmbed($"`{guildAuthor.Guild.Name}` Explicit Username/Nickname Detected",
                        $"Explicit content has been detected in your username/nickname.\n" +
                        $"Please change your username/nickname to continue using {guildAuthor.Guild.Name}", Color.Red)); // TODO - config
                }

                if (!IsMessageValid(guild, msg.Content))
                {
                    await AutoPunishUser(guildAuthor, "Explicit message");
                    await msg.DeleteAsync();
                    await user.XP.ExtendXPCooldown();
                }
            }
            catch (Exception e)
            {
                await msg.Channel.SendMessageAsync(e.Message + e.StackTrace + e.Data);
            }
        }
        public static bool IsMessageValid(Guild guild, string content)
        {
            if (content is null) return true;

            var defaultBannedWords = BannedWords.GetWords();
            var defaultBannedLinks = BannedLinks.GetLinks();
            var customBannedWords = guild.Moderation.Auto.CustomBanWords;
            var customBannedLinks = guild.Moderation.Auto.CustomBanLinks;

            var banWords = guild.Moderation.Auto.UseDefaultBanWords ? defaultBannedWords.Concat(customBannedWords) : customBannedWords;
            var banLinks = guild.Moderation.Auto.UseDefaultBanLinks ? defaultBannedLinks.Concat(customBannedLinks) : customBannedLinks;

            var lowerCaseMsg = content.ToLower();

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
                case int warnings when (warnings >= guild.Moderation.Auto.WarningsForBan && guild.Moderation.Auto.WarningsForBan > 0):
                    await user.BanAsync(TimeSpan.FromDays(-1), reason);
                    break;
                case int warnings when (warnings >= guild.Moderation.Auto.WarningsForKick && guild.Moderation.Auto.WarningsForKick > 0):
                    await user.KickAsync(reason);
                    break;
                default:
                    await user.WarnAsync(reason);
                    break;
            }
        }
    }
}