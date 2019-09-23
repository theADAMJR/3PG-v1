using Discord;
using Discord.WebSocket;
using System;
using Bot3PG.DataStructs;
using Bot3PG.Core.Data;
using System.Threading.Tasks;
using System.Collections;
using Discord.Rest;
using System.Linq;

namespace Bot3PG.Modules.XP
{
    public class LevelingSystem
    {
        public static async void ValidateForXPAsync(SocketUserMessage message)
        {
            if (message is null || !(message.Author is SocketGuildUser socketGuildUser)) return;

            var user = await Users.GetAsync(socketGuildUser);
            var guild = await Guilds.GetAsync(socketGuildUser.Guild);

            var userInCooldown = await user.XP.GetInXPCooldown();
            if (user is null || guild is null || userInCooldown || message.Content.Length <= guild.XP.MessageLengthThreshold) return;

            bool inDuplicateMessageDelay = user.XP.LastXPMsg.Add(guild.XP.DuplicateMessageThreshold) > DateTime.Now;
            if (user.Status.LastMessage == message.Content && inDuplicateMessageDelay) return;

            user.Status.LastMessage = message.Content;

            user.XP.LastXPMsg = DateTime.Now;

            uint oldLevel = user.XP.LevelNumber;
            user.XP.EXP += guild.XP.EXPPerMessage;
            uint newLevel = user.XP.LevelNumber;

            if (oldLevel != newLevel)
            {
                var embed = new EmbedBuilder();
                embed.WithColor(Color.Green);
                embed.WithTitle("✨ **LEVEL UP!**");
                embed.WithDescription(socketGuildUser.Mention + " just leveled up!");
                embed.AddField("LEVEL", newLevel, true);
                embed.AddField("XP", user.XP.EXP, true);

                bool newRoleGiven = await ValidateNewXPRoleAsync(socketGuildUser, guild, oldLevel, newLevel);
                if (newRoleGiven)
                {
                    embed.AddField("PROMOTION",
                        $"**Old:** {guild.XP.RoleRewards[oldLevel]?.Mention ?? "No role"}\n" +
                        $"**New:** {guild.XP.RoleRewards[newLevel].Mention}");
                }
                await message.Channel.SendMessageAsync("", embed: embed.Build());
            }
            await Users.Save(user);
        }

        public static async Task<bool> ValidateNewXPRoleAsync(SocketGuildUser socketGuildUser, Guild guild, uint oldLevel, uint newLevel)
        {
            if (!guild.XP.RoleRewards.Enabled || !guild.XP.RoleRewards.RolesExist) return false;

            if (!NewRoleRequired(socketGuildUser, guild, newLevel)) return false;

            if (!guild.XP.RoleRewards.StackRoles)
            {
                var oldXPRole = guild.XP.RoleRewards[oldLevel];
                await socketGuildUser.RemoveRoleAsync(oldXPRole);
            }
            var newXPRole = guild.XP.RoleRewards[newLevel];
            await socketGuildUser.AddRoleAsync(newXPRole);
            return true;
        }

        public static bool NewRoleRequired(SocketGuildUser socketGuildUser, Guild guild, uint newLevel)
        {
            ulong? levelRoleId = guild.XP.RoleRewards[newLevel]?.Id;
            return !socketGuildUser.Roles.Any(r => r.Id == levelRoleId);
        }
    }
}