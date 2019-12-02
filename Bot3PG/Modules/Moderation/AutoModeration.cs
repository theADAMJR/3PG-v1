using Bot3PG.Core.Data;
using Bot3PG.DataStructs;
using Bot3PG.Handlers;
using Discord;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Bot3PG.Modules.Moderation
{
    public static class AutoModeration
    {
        public static async Task ValidateMessage(SocketMessage message)
        {
            try
            {
                if (message is null || !(message.Author is SocketGuildUser guildAuthor) || guildAuthor.IsBot) return;

                var user = await Users.GetAsync(guildAuthor);
                var guild = await Guilds.GetAsync(guildAuthor.Guild);

                if (user.Status.LastMessage == message.Content)
                {
                    var messages = await message.Channel.GetMessagesAsync(100).FirstOrDefault();
                    var messageCount = messages.Where(m => m.Author == guildAuthor && m.Content == message.Content).Count();

                    if (messageCount >= guild.Moderation.Auto.SpamThreshold)
                    {
                        await message.Channel.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed("Slow down...", 
                            $"{message.Author.Mention}, you are sending messages too fast!", Color.Orange));
                    }
                    else if (messageCount >= guild.Moderation.Auto.SpamThreshold + 5)
                    {
                        await AutoPunishUser(guildAuthor, "Spamming duplicate messages");
                    }
                }
                user.Status.LastMessage = message.Content;

                if (!IsContentValid(guild, message.Content))
                {
                    await AutoPunishUser(guildAuthor, "Explicit message");
                    await message.DeleteAsync();
                    await user.XP.ExtendXPCooldown();
                }
                await Users.Save(user);
            }
            catch (Exception error)
            {
                error.Source = "Moderation";
                throw error;
            }
        }
        public static bool IsContentValid(Guild guild, string content)
        {
            if (content is null) return true;

            var defaultBannedWords = BannedWords.GetWords();
            var defaultBannedLinks = BannedLinks.GetLinks();
            var customBannedWords = guild.Moderation.Auto.CustomBanWords;
            var customBannedLinks = guild.Moderation.Auto.CustomBanLinks;

            var banWords = guild.Moderation.Auto.UseDefaultBanWords ? defaultBannedWords.Concat(customBannedWords).ToList() : customBannedWords;
            var banLinks = guild.Moderation.Auto.UseDefaultBanLinks ? defaultBannedLinks.Concat(customBannedLinks).ToList() : customBannedLinks;

            string lowerCaseContent = content.ToLower();
            return !(banWords.Any(w => lowerCaseContent.Contains(w)) || banLinks.Any(w => lowerCaseContent.Contains(w)));
        }

        public static async Task ValidateUsername(Guild guild, SocketGuildUser socketGuildUser)
        {
            if (!IsContentValid(guild, socketGuildUser.Nickname) || !IsContentValid(guild, socketGuildUser.Username))
            {
                var dmChannel = await socketGuildUser.GetOrCreateDMChannelAsync();
                await dmChannel.SendMessageAsync(embed: await EmbedHandler.CreateSimpleEmbed($"`{socketGuildUser.Guild.Name}` Explicit Username/Nickname Detected",
                    $"Explicit content has been detected in your username/nickname.\n" +
                    $"Please change your username/nickname to continue using {socketGuildUser.Guild.Name}", Color.Red)); // TODO - config
            }
        }

        public static async Task AutoPunishUser(SocketGuildUser socketGuildUser, string reason)
        {
            var guild = await Guilds.GetAsync(socketGuildUser.Guild);

            if (socketGuildUser.GuildPermissions.Administrator) return;

            var user = await Users.GetAsync(socketGuildUser);

            switch (user.Status.WarningsCount)
            {
                case int warnings when (warnings >= guild.Moderation.Auto.WarningsForBan && guild.Moderation.Auto.WarningsForBan > 0):
                    await user.BanAsync(TimeSpan.FromDays(-1), reason, Global.Client.CurrentUser);
                    break;
                case int warnings when (warnings >= guild.Moderation.Auto.WarningsForKick && guild.Moderation.Auto.WarningsForKick > 0):
                    await user.KickAsync(reason, Global.Client.CurrentUser);
                    break;
            }
            bool userAlreadyNotified = user.Status.WarningsCount >= guild.Moderation.Auto.WarningsForKick
                || user.Status.WarningsCount >= guild.Moderation.Auto.WarningsForBan;
            await user.WarnAsync(reason, Global.Client.CurrentUser, !userAlreadyNotified);
        }
    }
}