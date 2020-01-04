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
        public enum FilterType { BadWords, BadLinks, EmojiSpam, MassMention, DiscordInvites, AllCaps }

        public static async Task ValidateMessage(SocketMessage message)
        {
            try
            {
                if (message is null || !(message.Author is SocketGuildUser guildAuthor) || guildAuthor.IsBot) return;

                var user = await Users.GetAsync(guildAuthor);
                var guild = await Guilds.GetAsync(guildAuthor.Guild);
                var autoMod = guild.Moderation.Auto;

                var exemptRoles = autoMod.ExemptRoles.Select(id => guildAuthor.Guild.GetRole(id));
                bool userIsExempt = exemptRoles.Any(role => guildAuthor.Roles.Any(r => r.Id == role.Id));
                if (userIsExempt) return;

                if (autoMod.SpamNotification)
                {
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
                user.Status.LastMessage = message.Content;

                if (GetContentValidation(guild, message.Content) != null)
                {
                    await PunishUser(guildAuthor, "Explicit message");
                    try { await message.DeleteAsync(); } // 404 - there may be other auto mod bots -> message already deleted
                    catch {}
                    finally { await user.XP.ExtendXPCooldown(); }
                }
                await Users.Save(user);
            }
            catch (Exception ex) { await message.Channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Auto Moderation", ex.Message)); }
        }
        
        public static FilterType? GetContentValidation(Guild guild, string content)
        {
            if (content is null) return null;

            var autoMod = guild.Moderation.Auto;
            bool HasFilter(FilterType filter) => autoMod.Filters.Any(f => f == filter);
            
            bool hasHalfEmojis = content.Remove(0, content.Length / 2).All(c => char.IsSymbol(c));
            const int maxAtSigns = 5;
            
            if (HasFilter(FilterType.BadWords) && ContentIsExplicit(guild, content)) return FilterType.BadWords;
            if (HasFilter(FilterType.BadLinks) && ContentIsExplicit(guild, content)) return FilterType.BadLinks;
            if (HasFilter(FilterType.AllCaps) && content.All(c => char.IsUpper(c))) return FilterType.AllCaps;
            if (HasFilter(FilterType.DiscordInvites) && content.Contains("discord.gg")) return FilterType.DiscordInvites;
            if (HasFilter(FilterType.EmojiSpam) && hasHalfEmojis) return FilterType.EmojiSpam;
            if (HasFilter(FilterType.MassMention) && content.Count(c => c == '@') >= maxAtSigns) return FilterType.MassMention;

            return null;
        }

        public static bool ContentIsExplicit(Guild guild, string content)
        {           
            if (content is null) return false;        

            var autoMod = guild.Moderation.Auto;
            var badWords = BannedWords.Words;
            var badLinks = BannedWords.Links;
            var customBadWords = autoMod.CustomBanWords;
            var customBadLinks = autoMod.CustomBanLinks;

            var banWords = autoMod.UseDefaultBanWords ? badWords.Concat(customBadWords) : customBadWords;
            var banLinks = autoMod.UseDefaultBanLinks ? badLinks.Concat(customBadLinks) : customBadLinks;

            string lowerCaseContent = content.ToLower();
            var words = content.ToLower().Split(" ");

            return banWords.Concat(banLinks).Any(w => words.Contains(w));
        }

        public static async Task ValidateUsername(Guild guild, SocketGuildUser socketGuildUser)
        {
            if (ContentIsExplicit(guild, socketGuildUser.Nickname) || ContentIsExplicit(guild, socketGuildUser.Username))
            {
                var user = await Users.GetAsync(socketGuildUser);

                if (guild.Moderation.Auto.ResetNickname)
                {
                    try { await socketGuildUser.ModifyAsync(u => u.Nickname = socketGuildUser.Username); }
                    catch {}
                }

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

        public static async Task PunishUser(SocketGuildUser socketGuildUser, string reason)
        {
            var guild = await Guilds.GetAsync(socketGuildUser.Guild);
            var autoMod = guild.Moderation.Auto;

            if (socketGuildUser.GuildPermissions.Administrator) return;

            var user = await Users.GetAsync(socketGuildUser);
            int warnings = user.Status.WarningsCount;

            if (warnings >= autoMod.WarningsForBan && autoMod.WarningsForBan > 0)
                await user.BanAsync(TimeSpan.FromDays(-1), reason, Global.Client.CurrentUser);                
            else if (warnings >= autoMod.WarningsForKick && autoMod.WarningsForKick > 0)
                await user.KickAsync(reason, Global.Client.CurrentUser);
            else
                await user.WarnAsync(reason, Global.Client.CurrentUser);
        }
    }
}