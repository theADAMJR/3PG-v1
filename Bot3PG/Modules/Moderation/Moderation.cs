using Bot3PG.Handlers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using Bot3PG.Core.Data;
using System;
using Bot3PG.Moderation;
using System.Linq;
using System.Collections.Generic;
using Bot3PG.DataStructs;
using Bot3PG.Utilities;

namespace Bot3PG.Modules.Moderation
{
    [RequireContext(ContextType.Guild)]
    public sealed class Moderation : CommandBase
    {
        [Command("Kick")]
        [Summary("Kick a user [with reason]")]
        [RequireUserPermission(GuildPermission.KickMembers), RequireBotPermission(GuildPermission.KickMembers)]
        public async Task KickUser(SocketGuildUser target, [Remainder] string reason = "No reason provided.")
        {
            var user = await Users.GetAsync(target);
            if (target.GuildPermissions.Administrator)
            {
                await ReplyAsync(await EmbedHandler.CreateBasicEmbed("Moderation", $"Admins can't be kicked.", Color.Red));
                return;
            }
            await user.KickAsync(reason);
        }

        [Command("Ban")]
        [Summary("Ban a user [with reason]")]
        [RequireUserPermission(GuildPermission.BanMembers), RequireBotPermission(GuildPermission.BanMembers)]
        [Remarks("**Accepted Values:** s/sec m/min h/hour d/day w/week mo/month y/year")]
        public async Task BanUser(SocketGuildUser target, string duration = "1d", [Remainder] string reason = "No reason provided.")
        {
            var user = await Users.GetAsync(target);
            var banDuration = CommandUtilities.ParseDuration(duration);

            if (user.Status.IsBanned)
            {
                await ReplyAsync(await EmbedHandler.CreateBasicEmbed("Moderation", $"User already banned.", Color.Red));
                return;
            }
            else
            {
                var targetHighest = target.Hierarchy;
                var senderHighest = (Context.Message.Author as SocketGuildUser).Hierarchy;

                if (targetHighest < senderHighest)
                {
                    await user.BanAsync(banDuration, reason);
                }
                else
                {
                    await ReplyAsync(await EmbedHandler.CreateBasicEmbed("Moderation", $"Higher rank user cannot be banned.", Color.Red));
                    return;
                }
            }
        }

        [Command("Unban")]
        [Summary("Unban a user [with reason]")]
        [RequireUserPermission(GuildPermission.BanMembers), RequireBotPermission(GuildPermission.BanMembers)]
        public async Task UnbanUser(SocketGuildUser target, [Remainder] string reason = "No reason provided.")
        {
            var user = await Users.GetAsync(target);
            var restBan = await Context.Guild.GetBanAsync(target);
            var unbanTarget = Context.Guild.GetUser(restBan.User.Id);

            if (unbanTarget.GuildPermissions.Administrator)
            {
                await ReplyAsync(await EmbedHandler.CreateBasicEmbed("Moderation", $"Admins can't be banned.", Color.Red));
                return;
            }

            if (!user.Status.IsBanned)
            {
                await ReplyAsync(await EmbedHandler.CreateBasicEmbed("Moderation", $"User not currently banned.", Color.Red));
                return;
            }
            else
            {
                var targetHighest = unbanTarget.Hierarchy;
                var senderHighest = (Context.Message.Author as SocketGuildUser).Hierarchy;
                
                if (targetHighest < senderHighest)
                {
                    await user.UnbanAsync(reason);
                }
                else
                {
                    await ReplyAsync(await EmbedHandler.CreateBasicEmbed("Moderation", $"Higher rank user cannot be unbanned.", Color.Red));
                    return;
                }
            }
            if (user is null)
            {
                await ReplyAsync(await EmbedHandler.CreateBasicEmbed("Moderation", $"User account not found.", Color.Red));
                return;
            }
            await user.UnbanAsync(reason);
        }

        [Command("Mute")]
        [RequireUserPermission(GuildPermission.MuteMembers), RequireBotPermission(GuildPermission.MuteMembers)]
        [Summary("Mute a user's voice and chat [with reason] ")]
        public async Task MuteUser(SocketGuildUser target, string duration = "1d", [Remainder] string reason = "No reason provided.")
        {
            var user = await Users.GetAsync(target);
            var muteDuration = CommandUtilities.ParseDuration(duration);

            if (target.GuildPermissions.Administrator)
            {
                await ReplyAsync(await EmbedHandler.CreateBasicEmbed("Moderation", $"Admins can't be muted.", Color.Red));
                return;
            }

            if (!target.IsMuted)
            {
                await user.MuteAsync(muteDuration, reason);
            }
            else
            {
                await ReplyAsync(await EmbedHandler.CreateBasicEmbed("Moderation", $"User is already muted.", Color.Red));
            }
        }

        [Command("Unmute")]
        [RequireUserPermission(GuildPermission.MuteMembers), RequireBotPermission(GuildPermission.MuteMembers)]
        [Summary("Unmute a user's voice and chat [with reason]")]
        public async Task UnmuteUser(SocketGuildUser target, [Remainder] string reason = "No reason provided.")
        {
            var user = await Users.GetAsync(target);

            if (target.IsMuted)
            {
                await user.UnmuteAsync(reason);
                return;
            }
            await ReplyAsync(await EmbedHandler.CreateBasicEmbed("Moderation", $"User account not found.", Color.Red));
        }

        [Command("Warn")]
        [RequireUserPermission(GuildPermission.BanMembers), RequireBotPermission(GuildPermission.BanMembers)]
        [Summary("Warn a user [with reason] and add a warning to their account")]
        public async Task WarnUser(SocketGuildUser target, [Remainder] string reason = "No reason provided.")
        {
            var user = await Users.GetAsync(target);
            if (target.GuildPermissions.Administrator)
            {
                await ReplyAsync(await EmbedHandler.CreateBasicEmbed("Moderation", $"Admins can't be warned.", Color.Red));
                return;
            }
            await user.WarnAsync(reason);
        }

        [Command("User")]
        [RequireUserPermission(GuildPermission.BanMembers), RequireBotPermission(GuildPermission.BanMembers)]
        [Summary("Display target user details")]
        public async Task Account(SocketGuildUser target = null, [Remainder] string args2 = "")
        {
            var accountTarget = target ?? Context.User as SocketGuildUser;

            if (accountTarget.IsBot)
            {
                await ReplyAsync(await EmbedHandler.CreateBasicEmbed("Moderation", "Bots don't have accounts!", Color.Red));
                return;
            }

            var user = await Users.GetAsync(accountTarget);
            if (args2 == "reset")
            {
                if (target is null)
                {
                    await ReplyAsync(await EmbedHandler.CreateBasicEmbed("Moderation", "User not found", Color.Red));
                    return;
                }
                else
                {
                    Users.ResetAsync(target);
                    await ReplyAsync(await EmbedHandler.CreateBasicEmbed("Moderation", "User account reset", Color.Orange));
                    return;
                }
            }
            
            var embed = new EmbedBuilder();
            embed.ThumbnailUrl = accountTarget.GetAvatarUrl();
            embed.Color = Color.Orange;
            embed.AddField("Agreed To Rules", user.Status.AgreedToRules, inline: true);
            embed.AddField("Warnings", user.Status.WarningsCount, inline: true);

            embed.AddField("Is Banned", user.Status.IsBanned, inline: true);
            if (user.Status.IsBanned)
            {
                var ban = user.Status[PunishmentType.Ban];
                embed.AddField("Ban Reason", ban.Reason, inline: true);
                embed.AddField("Start of Ban", ban.Start, inline: true);
                embed.AddField("End of Ban", ban.End, inline: true);
            }

            embed.AddField("Is Muted", user.Status.IsMuted, inline: true);
            if (user.Status.IsMuted)
            {
                var mute = user.Status[PunishmentType.Mute];
                embed.AddField("Mute Reason", mute.Reason, inline: true);
                embed.AddField("Start of Mute", mute.Start, inline: true);
                embed.AddField("End of Mute", mute.End, inline: true);
            }

            var userInCooldown = await user.XP.GetInXPCooldown();
            embed.AddField("In XP Cooldown", userInCooldown, inline: true);
            await ReplyAsync(embed);
        }

        [Command("Punishments")]
        [RequireUserPermission(GuildPermission.BanMembers), RequireBotPermission(GuildPermission.BanMembers)]
        [Summary("List punishments of a user"), Remarks("**Types:** Ban, Mute, Warn, Kick \n**Order:** Descending / Ascending")]
        public async Task Bans(SocketGuildUser target = null, string type = "", string orderBy = "desc")
        {
            target ??= Context.User as SocketGuildUser;
            var user = await Users.GetAsync(target);

            Enum.TryParse(typeof(PunishmentType), type, ignoreCase: true, out var parsedPunishment);
            var punishment = (PunishmentType?)parsedPunishment;

            var embed = new EmbedBuilder();

            var allPunishments = (orderBy == "desc" || orderBy == "descending") ? user.Status.Punishments.AsEnumerable().Reverse() : user.Status.Punishments;
            var punishments = string.IsNullOrEmpty(type) ? allPunishments.ToList() : allPunishments.Where(p => p.Type == punishment).ToList();

            foreach (var log in punishments)
            {
                var index = punishments.IndexOf(log);
                embed.AddField($"*[{index + 1}]* **{log.Type}**",
                    $"__Reason:__ {log.Reason}\n" +
                    $"__Start:__ {log.Start.ToShortDateString()} {log.Start.ToShortTimeString()}\n" +
                    $"__End:__ {log.End.ToShortDateString()} {log.End.ToShortTimeString()}", inline: false);
            }
            await ReplyAsync(embed);
        }
    }
}