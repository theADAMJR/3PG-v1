using Bot3PG.Data;
using Bot3PG.Data.Structs;
using Bot3PG.Handlers;
using Bot3PG.Utils;
using Discord;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Bot3PG.Modules.Moderation
{
    public static class Auto
    {
        public static async Task ValidateMessage(SocketMessage message)
        {
            try
            {
                if (message is null || !(message.Author is SocketGuildUser guildAuthor) || guildAuthor.IsBot) return;

                var user = await Users.GetAsync(guildAuthor);
                var guild = await Guilds.GetAsync(guildAuthor.Guild);
                var autoMod = guild.Moderation.Auto;

                ValidateUserNotExempt(guildAuthor, guild);

                if (autoMod.SpamNotification)
                    await SendSpamNotification(message, guildAuthor, guild);

                var messageValidation = GetContentValidation(guild, message.Content, user);
                if (messageValidation != null)
                {
                    var filter = guild.Moderation.Auto.Filters.FirstOrDefault(f => f.Filter == messageValidation);
                    await PunishUser(filter.Punishment, user, messageValidation.ToString().ToSentenceCase());
                    try { await message.DeleteAsync(); } // 404 - there may be other auto mod bots -> message already deleted
                    catch {}
                    finally { await user.XP.ExtendXPCooldown(); }
                }
                user.Status.LastMessage = message.Content;
                await Users.Save(user);
            }
            catch (Exception ex) { await message.Channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Auto Moderation", ex.Message)); }
        }

        private static async Task SendSpamNotification(SocketMessage message, SocketGuildUser guildAuthor, Guild guild)
        {
            var autoMod = guild.Moderation.Auto;
            var messages = await message.Channel.GetMessagesAsync(25).FirstOrDefault();
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

        private static void ValidateUserNotExempt(SocketGuildUser guildAuthor, Guild guild)
        {
            var exemptRoles = guild.Moderation.Auto.ExemptRoles.Select(id => guildAuthor.Guild.GetRole(id));
            bool userIsExempt = exemptRoles.Any(role => guildAuthor.Roles.Any(r => r.Id == role.Id));
            if (userIsExempt) 
                throw new InvalidOperationException("User is exempt from auto moderation.");
        }

        public static FilterType? GetContentValidation(Guild guild, string content, GuildUser user)
        {
            if (string.IsNullOrEmpty(content)) return null;

            var autoMod = guild.Moderation.Auto;
            Func<FilterType, bool> HasFilter = (FilterType filter) => autoMod.Filters.FirstOrDefault(f => f.Filter == filter) != null;
            
            if (HasFilter(FilterType.BadWords) && ContentIsExplicit(guild, content)) return FilterType.BadWords;
            if (HasFilter(FilterType.BadLinks) && ContentIsExplicit(guild, content, links: true)) return FilterType.BadLinks;

            bool hasExcessiveCaps = content.All(c => char.IsUpper(c)) && content.Length > 5; 
            if (HasFilter(FilterType.AllCaps) && hasExcessiveCaps) return FilterType.AllCaps;
            if (HasFilter(FilterType.DiscordInvites) && content.Contains("discord.gg")) return FilterType.DiscordInvites;

            bool hasHalfEmojis = content.Remove(0, content.Length / 2).All(c => char.IsSymbol(c));
            if (HasFilter(FilterType.EmojiSpam) && hasHalfEmojis) return FilterType.EmojiSpam;

            const int maxAtSigns = 5;
            if (HasFilter(FilterType.MassMention) && content.Count(c => c == '@') >= maxAtSigns) return FilterType.MassMention;
            if (HasFilter(FilterType.DuplicateMessage) && content.ToLower() == user.Status.LastMessage && !string.IsNullOrEmpty(content)) return FilterType.DuplicateMessage;
            
            bool containsZalgo = Regex.IsMatch(content, @"([^\u0009-\u02b7\u2000-\u20bf\u2122\u0308]|(?![^aeiouy])\u0308)", RegexOptions.Multiline);
            if (HasFilter(FilterType.Zalgo) && containsZalgo) return FilterType.Zalgo;

            return null;
        }

        public static bool ContentIsExplicit(Guild guild, string content, bool links = false)
        {
            if (string.IsNullOrEmpty(content)) return false;

            var autoMod = guild.Moderation.Auto;
            var badWords = BannedWords.Words;
            var badLinks = BannedWords.Links;
            var customBadWords = autoMod.CustomBanWords;
            var customBadLinks = autoMod.CustomBanLinks;

            var banWords = autoMod.UseDefaultBanWords ? badWords.Concat(customBadWords) : customBadWords;
            var banLinks = autoMod.UseDefaultBanLinks ? badLinks.Concat(customBadLinks) : customBadLinks;

            string lowerCaseContent = content.ToLower();
            var words = content.ToLower().Split(" ");

            return banWords.Any(w => words.Contains(w)) || links && banLinks.Any(l => content.Contains(l));
        }

        public static async Task ValidateUsername(Guild guild, SocketGuildUser oldUser)
        {
            try
            {
                var guildUser = oldUser.Guild.GetUser(oldUser.Id);
                if (!ContentIsExplicit(guild, guildUser.Nickname) && !ContentIsExplicit(guild, guildUser.Username)) return;

                var user = await Users.GetAsync(guildUser);
                if (guild.Moderation.Auto.ResetNickname)
                {
                    try { await guildUser.ModifyAsync(u => u.Nickname = guildUser.Username); }
                    catch {}
                }
                var dmChannel = await guildUser.GetOrCreateDMChannelAsync();

                await PunishUser(guild.Moderation.Auto.ExplicitUsernamePunishment, user, "Explicit display name");                
                await user.WarnAsync("Explicit display name", Global.Client.CurrentUser);            
            }
            catch (Exception) {}
        }

        public static async Task PunishUser(PunishmentType punishment, GuildUser user, string reason = null)
        {
            switch (punishment)
            {
                case PunishmentType.Ban:
                    await user.BanAsync(TimeSpan.MaxValue, reason, Global.Client.CurrentUser);
                    return;
                case PunishmentType.Kick:
                    await user.KickAsync(reason, Global.Client.CurrentUser);
                    return;
                case PunishmentType.Mute:
                    await user.MuteAsync(TimeSpan.MaxValue, reason, Global.Client.CurrentUser);
                    return;
                case PunishmentType.Warn:
                    await user.WarnAsync(reason, Global.Client.CurrentUser);
                    return;
            }
        }
    }
}