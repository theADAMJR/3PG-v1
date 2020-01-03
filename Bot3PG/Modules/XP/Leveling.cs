using Discord;
using Discord.WebSocket;
using System;
using Bot3PG.Data;
using System.Threading.Tasks;
using System.Collections;
using System.Linq;
using Bot3PG.Data.Structs;

namespace Bot3PG.Modules.XP
{
    public class Leveling
    {
        public static async void ValidateForXPAsync(SocketUserMessage message)
        {
            if (message is null || !(message.Author is SocketGuildUser socketGuildUser)) return;

            var user = await Users.GetAsync(socketGuildUser);
            var guild = await Guilds.GetAsync(socketGuildUser.Guild);
            var xp = guild.XP;

            bool inCooldown = await user.XP.GetXPCooldown();
            if (user is null || guild is null || inCooldown || message.Content.Length <= xp.MessageLengthThreshold) return;

            bool inSpamCooldown = user.XP.LastXPMsg.AddSeconds(xp.DuplicateMessageThreshold) > DateTime.Now;
            bool channelIsBlacklisted = guild.XP.ExemptChannels.Any(id => id == message.Channel.Id);
            bool roleIsBlackListed = guild.XP.ExemptRoles.Any(id => socketGuildUser.Roles.Any(r => r.Id == id));

            if (channelIsBlacklisted || roleIsBlackListed || user.Status.LastMessage == message.Content && inSpamCooldown) return;

            user.Status.LastMessage = message.Content;
            user.XP.LastXPMsg = DateTime.Now;

            int oldLevel = user.XP.Level;
            user.XP.EXP += guild.XP.EXPPerMessage;
            int newLevel = user.XP.Level;
            user.Status.MessageCount++;

            if (oldLevel != newLevel)
            {
                var embed = new EmbedBuilder();
                embed.WithColor(Color.Green);
                embed.WithTitle("✨ **LEVEL UP!**");
                embed.WithDescription(xp.Messages.Method == MessageMethod.DM ? $"{socketGuildUser.Mention}, you leveled up!" : $"{socketGuildUser.Mention} just leveled up!");
                embed.AddField("LEVEL", newLevel, true);
                embed.AddField("XP", user.XP.EXP, true);

                bool newRoleGiven = await ValidateNewXPRoleAsync(socketGuildUser, guild, oldLevel, newLevel);
                if (newRoleGiven && xp.Messages.Method != MessageMethod.DM)
                {
                    embed.AddField("PROMOTION", $"**New:** {xp.RoleRewards[newLevel].Mention}");
                    embed.WithFooter(socketGuildUser.Guild.Name);
                }    
                else if (newRoleGiven)
                {
                    // false if xp role was null (if assigned role was deleted)
                    embed.AddField("PROMOTION", "You've also received a new role!");
                }       
                switch (xp.Messages.Method)
                {
                    case MessageMethod.DM:
                        if (socketGuildUser.IsBot) return;
                        await socketGuildUser.SendMessageAsync(embed: embed.Build());
                        break;
                    case MessageMethod.SpecificChannel:
                        var channel = socketGuildUser.Guild.GetTextChannel(xp.Messages.XPChannel);
                        await channel.SendMessageAsync(embed: embed.Build());
                        break;                    
                    case MessageMethod.AnyChannel:
                        await message.Channel.SendMessageAsync(embed: embed.Build());
                        break;
                }
            }
            await Users.Save(user);
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