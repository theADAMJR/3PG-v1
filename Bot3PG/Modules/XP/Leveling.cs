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
        public static async void ValidateForEXPAsync(SocketUserMessage message, Guild guild)
        {
            try
            {
                var guildUser = await ValidateCanEarnEXP(message, guild);
                var user = await Users.GetAsync(message.Author);

                guildUser.Status.LastMessage = message.Content;
                guildUser.XP.LastXPMsg = DateTime.Now;

                int oldLevel = guildUser.XP.Level;
                guildUser.XP.EXP += guild.XP.EXPPerMessage;

                int newLevel = guildUser.XP.Level;
                guildUser.Status.MessageCount++;
                user.MessageCount++;

                await Users.Save(guildUser);
                await Users.Save(user);

                if (oldLevel != newLevel)
                    await SendLevelUpMessageAsync(message, guild, guildUser, oldLevel, newLevel);
            }
            catch (InvalidOperationException) {}
            catch (Exception ex) { await Debug.LogCriticalAsync("Leveling", ex.Message, ex); }
        }

        public static async Task<GuildUser> ValidateCanEarnEXP(SocketUserMessage message, Guild guild)
        {
            if (message is null || guild is null || !(message.Author is SocketGuildUser guildAuthor))
                throw new InvalidOperationException("Message author could not be found.");

            var guildUser = await Users.GetAsync(guildAuthor);

            bool inCooldown = await guildUser.XP.GetXPCooldown();
            if (guildUser is null || inCooldown || message.Content.Length <= guild.XP.MessageLengthThreshold)
                throw new InvalidOperationException("User cannot earn EXP.");

            bool channelIsBlacklisted = guild.XP.ExemptChannels.Any(id => id == message.Channel.Id);
            bool roleIsBlackListed = guild.XP.ExemptRoles.Any(id => guildAuthor.Roles.Any(r => r.Id == id));
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
                embed.AddField("PROMOTION", $"**New:** {guild.XP.RoleRewards[newLevel]?.Mention}");
                embed.WithFooter(socketGuildUser.Guild.Name);
            }
            else if (newRoleGiven)
                embed.AddField("PROMOTION", "You've also received a new role!");
            return embed.Build();
        }

        private static async Task SendLevelUpMessageAsync(SocketUserMessage message, Guild guild, GuildUser guildUser, int oldLevel, int newLevel)
        {
            var socketGuildUser = message.Author as SocketGuildUser;
            var embed = await GetLevelUpEmbed(socketGuildUser, guild, guildUser, oldLevel, newLevel);
            
            switch (guild.XP.Messages.Method)
            {
                case MessageMethod.DM:
                    if (socketGuildUser.IsBot) return;
                    try { await socketGuildUser.SendMessageAsync(embed: embed); }
                    catch (Exception) {}
                    break;
                case MessageMethod.SpecificChannel:
                    var channel = socketGuildUser.Guild.GetTextChannel(guild.XP.Messages.XPChannel);
                    try { await channel.SendMessageAsync(embed: embed); }
                    catch (Exception) {}
                    break;
                case MessageMethod.AnyChannel:
                    try { await message.Channel.SendMessageAsync(embed: embed); }
                    catch (Exception) {}
                    break;
            }
        }

        public static async Task<bool> ValidateNewXPRoleAsync(SocketGuildUser socketGuildUser, Guild guild, int oldLevel, int newLevel)
        {
            if (!guild.XP.RoleRewards.Enabled || !guild.XP.RoleRewards.RolesExist || !NewRoleRequired(socketGuildUser, guild, newLevel)) return false;

            if (!guild.XP.RoleRewards.StackRoles)
            {
                var oldXPRole = guild.XP.RoleRewards[oldLevel];
                await socketGuildUser.RemoveRoleAsync(oldXPRole);
            }
            var newXPRole = guild.XP.RoleRewards[newLevel];
            if (newXPRole is null) return false;
            
            await socketGuildUser.AddRoleAsync(newXPRole);
            return true;
        }

        public static bool NewRoleRequired(SocketGuildUser socketGuildUser, Guild guild, int newLevel)
        {
            ulong? levelRoleId = guild.XP.RoleRewards[newLevel]?.Id;
            return !socketGuildUser.Roles.Any(r => r.Id == levelRoleId);
        }
    }
}