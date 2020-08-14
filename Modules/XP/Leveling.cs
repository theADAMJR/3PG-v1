using Discord;
using Discord.WebSocket;
using System;
using Bot3PG.Data;
using System.Threading.Tasks;
using System.Collections;
using System.Linq;
using Bot3PG.Data.Structs;
using Bot3PG.Services;

namespace Bot3PG.Modules.XP
{
    public class Leveling
    {
        public static async Task ValidateForEXPAsync(IUserMessage message, Guild guild)
        {
            var guildUser = await ValidateCanEarnEXP(message, guild);
            var user = await Users.GetAsync(message.Author as SocketUser);

            guildUser.Status.LastMessage = message.Content;
            guildUser.XP.LastXPMsg = DateTime.Now;

            int oldLevel = guildUser.XP.Level;
            guildUser.XP.EXP += guild.XP.EXPPerMessage;

            int newLevel = guildUser.XP.Level;
            guildUser.Status.MessageCount++;
            user.MessageCount++;

            await GuildUsers.Save(guildUser);
            await Users.Save(user);

            if (oldLevel != newLevel)
                await SendLevelUpMessageAsync(message, guild, guildUser, oldLevel, newLevel);
        }

        private static async Task<GuildUser> ValidateCanEarnEXP(IUserMessage message, Guild guild)
        {
            if (message is null || guild is null || !(message.Author is IGuildUser guildAuthor))
                throw new InvalidOperationException();

            var guildUser = await GuildUsers.GetAsync(guildAuthor);

            bool inCooldown = await guildUser.XP.GetXPCooldown();
            if (guildUser is null || inCooldown || message.Content.Length <= guild.XP.MessageLengthThreshold)
                throw new InvalidOperationException("User cannot earn EXP.");

            bool channelIsBlacklisted = guild.XP.ExemptChannels.Any(id => id == message.Channel.Id);
            bool roleIsBlackListed = guild.XP.ExemptRoles.Any(id => guildAuthor.RoleIds.Any(roleId => roleId == id));
            if (channelIsBlacklisted || roleIsBlackListed)
                throw new InvalidOperationException("Channel or role cannot earn EXP.");

            return guildUser;
        }

        private static async Task<Embed> GetLevelUpEmbed(SocketGuildUser socketGuildUser, Guild guild, GuildUser guildUser, int oldLevel, int newLevel)
        {            
            var embed = new EmbedBuilder();
            embed.WithColor(Color.Green);
            embed.WithTitle("✨ **LEVEL UP!**");
            embed.WithDescription(guild.XP.Messages.Method == MessageMethod.DM ? $"{socketGuildUser.Mention}, you leveled up!" : $"{socketGuildUser.Mention} just leveled up!");
            embed.AddField("LEVEL", newLevel, true);
            embed.AddField("XP", guildUser.XP.EXP, true);

            bool newRoleGiven = await ValidateNewXPRoleAsync(socketGuildUser, guild, oldLevel, newLevel);
            if (newRoleGiven && guild.XP.Messages.Method != MessageMethod.DM)
            {
                embed.AddField("PROMOTION", $"**Old**: {GetOldLevelRole(guild, oldLevel)?.Mention ?? "N/A"}\n" +
                            $"**New**: {GetLevelRole(guild, newLevel)?.Mention}");
                embed.WithFooter(socketGuildUser.Guild.Name);
            }
            else if (newRoleGiven)
                embed.AddField("PROMOTION", "You've also received a new role!");
            return embed.Build();
        }

        private static async Task SendLevelUpMessageAsync(IUserMessage message, Guild guild, GuildUser guildUser, int oldLevel, int newLevel)
        {
            var guildAuthor = message.Author as SocketGuildUser;
            var embed = await GetLevelUpEmbed(guildAuthor, guild, guildUser, oldLevel, newLevel);
            
            switch (guild.XP.Messages.Method)
            {
                case MessageMethod.DM:
                    if (guildAuthor.IsBot) return;
                    try { await guildAuthor.SendMessageAsync(embed: embed); }
                    catch (Exception) {}
                    break;
                case MessageMethod.SpecificChannel:
                    var channel = guildAuthor.Guild.GetTextChannel(guild.XP.Messages.XPChannel);
                    try { await channel.SendMessageAsync(embed: embed); }
                    catch (Exception) {}
                    break;
                case MessageMethod.AnyChannel:
                    try { await message.Channel.SendMessageAsync(embed: embed); }
                    catch (Exception) {}
                    break;
            }
        }

        private static async Task<bool> ValidateNewXPRoleAsync(SocketGuildUser guildAuthor, Guild guild, int oldLevel, int newLevel)
        {
            var levelRole = GetLevelRole(guild, newLevel);
            if (!guild.XP.RoleRewards.Enabled || !guild.XP.RoleRewards.RolesExist || levelRole is null) return false;
            if (!guild.XP.RoleRewards.StackRoles)
            {
                var oldLevelRole = GetOldLevelRole(guild, oldLevel);
                if (oldLevelRole != null)
                    await guildAuthor.RemoveRoleAsync(oldLevelRole);
            }
            if (levelRole is null)
                return false;

            try { await guildAuthor.AddRoleAsync(levelRole); }
            catch (Exception ex) { await Debug.LogErrorAsync("leveling", "Tried to add role but could not.", ex); }
            return true;
        }

        private static SocketRole GetLevelRole(Guild guild, int newLevel)
        {
            var socketGuild = Global.Client.GetGuild(guild.ID);
            guild.XP.RoleRewards.LevelRoles.TryGetValue($"{newLevel}", out var levelRoleId);
            return socketGuild.Roles.FirstOrDefault(r => r.Id == levelRoleId);
        }

        private static SocketRole GetOldLevelRole(Guild guild, int oldLevel)
        {
            for (int i = oldLevel - 1; i >= 0 ; i--)
            {
                var levelRole = GetLevelRole(guild, oldLevel);
                if (levelRole != null) 
                    return levelRole;
            }
            return null;
        }
    }
}