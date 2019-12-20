using Bot3PG.Data;
using Bot3PG.Data.Structs;
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
                var autoMod = guild.Moderation.Auto;

                if (autoMod.SpamNotification)
                {
                    var messages = await message.Channel.GetMessagesAsync(100).FirstOrDefault();
                    var userMessages = messages.Where(m => m.Author == guildAuthor && m.Content == message.Content);
                    int messageCount = userMessages.Count(m => DateTime.Now - m.CreatedAt < TimeSpan.FromSeconds(60));

                    if (autoMod.SpamThreshold > 0 && messageCount >= autoMod.SpamThreshold)
                    {
                        var reminder = await message.Channel.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed("Slow down...", 
                            $"{message.Author.Mention}, you are sending messages too fast!", Color.Orange));
                        await Task.Delay(4000);
                        try { await reminder.DeleteAsync(); } // 404 => user may delete the reminder
                        catch {}
                    }
                }
                user.Status.LastMessage = message.Content;

                if (!IsContentValid(guild, message.Content))
                {
                    await AutoPunishUser(guildAuthor, "Explicit message");
                    try { await message.DeleteAsync(); } // 404 - there may be other auto mod bots -> message already deleted
                    catch {}
                    finally { await user.XP.ExtendXPCooldown(); }
                }
                await Users.Save(user);
            }
            catch (Exception ex) { await message.Channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Auto Moderation", ex.Message)); }
        }
        public static bool IsContentValid(Guild guild, string content)
        {
            if (content is null) return true;

            var defaultBannedWords = BannedWords.Words;
            var defaultBannedLinks = BannedLinks.Links;
            var customBannedWords = guild.Moderation.Auto.CustomBanWords;
            var customBannedLinks = guild.Moderation.Auto.CustomBanLinks;

            var banWords = guild.Moderation.Auto.UseDefaultBanWords ? defaultBannedWords.Concat(customBannedWords) : customBannedWords;
            var banLinks = guild.Moderation.Auto.UseDefaultBanLinks ? defaultBannedLinks.Concat(customBannedLinks) : customBannedLinks;

            string lowerCaseContent = content.ToLower();
            return !(banWords.Any(w => lowerCaseContent.Contains(w)) || banLinks.Any(w => lowerCaseContent.Contains(w)));
        }

        public static async Task ValidateUsername(Guild guild, SocketGuildUser socketGuildUser)
        {
            if (!IsContentValid(guild, socketGuildUser.Nickname) || !IsContentValid(guild, socketGuildUser.Username))
            {
                var user = await Users.GetAsync(socketGuildUser);

                var dmChannel = await socketGuildUser.GetOrCreateDMChannelAsync();
                switch (guild.Moderation.Auto.ExplicitUsernamePunishment)
                {
                    case PunishmentType.Ban:
                        await user.BanAsync(TimeSpan.MaxValue, "Explicit display name", Global.Client.CurrentUser);
                        return;                        
                    case PunishmentType.Kick:
                        await user.KickAsync("Explicit display name", Global.Client.CurrentUser);
                        return;
                    case PunishmentType.Mute:                  
                        await user.MuteAsync(TimeSpan.MaxValue, "Explicit display name", Global.Client.CurrentUser);
                        return;
                    case PunishmentType.Warn:
                        await user.WarnAsync("Explicit display name", Global.Client.CurrentUser);
                        return;
                }
                await user.WarnAsync("Explicit display name", Global.Client.CurrentUser);
                await dmChannel.SendMessageAsync(embed: await EmbedHandler.CreateSimpleEmbed($"`{socketGuildUser.Guild.Name}` - Explicit Display Name Detected",
                $"Explicit content has been detected in your display name.\n" +
                $"Please change your display name to continue using {socketGuildUser.Guild.Name}", Color.Red)); // TODO - config    
            }
        }

        public static async Task AutoPunishUser(SocketGuildUser socketGuildUser, string reason)
        {
            var guild = await Guilds.GetAsync(socketGuildUser.Guild);
            var autoMod = guild.Moderation.Auto;

            if (socketGuildUser.GuildPermissions.Administrator) return;

            var user = await Users.GetAsync(socketGuildUser);
            switch (user.Status.WarningsCount)
            {
                case int warnings when (warnings >= autoMod.WarningsForBan && autoMod.WarningsForBan > 0):
                    await user.BanAsync(TimeSpan.FromDays(-1), reason, Global.Client.CurrentUser);
                    return;
                case int warnings when (warnings >= autoMod.WarningsForKick && autoMod.WarningsForKick > 0):
                    await user.KickAsync(reason, Global.Client.CurrentUser);
                    return;
            }
            bool userAlreadyNotified = user.Status.WarningsCount >= autoMod.WarningsForKick || user.Status.WarningsCount >= autoMod.WarningsForBan;
            await user.WarnAsync(reason, Global.Client.CurrentUser, !userAlreadyNotified);
        }
    }
}