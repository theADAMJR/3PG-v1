using Bot3PG.Handlers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using Bot3PG.Core.Data;
using System;
using Bot3PG.Moderation;

namespace Bot3PG.Modules.Moderation
{
    [RequireContext(ContextType.Guild)]
    public sealed class Moderation : CommandBase
    {
        [Command("Kick")]
        [Summary("Kick a user [with reason]")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        [RequireBotPermission(GuildPermission.KickMembers)]
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
        [RequireUserPermission(GuildPermission.BanMembers)]
        [RequireBotPermission(GuildPermission.BanMembers)]
        [Remarks("**Accepted Values:** s/sec m/min h/hour d/day w/week mo/month y/year")]
        public async Task BanUser(SocketGuildUser target, string duration = "7d", [Remainder] string reason = "No reason provided.")
        {
            var user = await Users.GetAsync(target);
            var banDuration = ParseDuration(duration);

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
        [RequireUserPermission(GuildPermission.BanMembers)]
        [RequireBotPermission(GuildPermission.BanMembers)]
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
        [RequireUserPermission(GuildPermission.MuteMembers)]
        [RequireBotPermission(GuildPermission.MuteMembers)]
        [Summary("Mute a user's voice and chat [with reason] ")]
        public async Task MuteUser(SocketGuildUser target, string duration, [Remainder] string reason = "No reason provided.")
        {
            var user = await Users.GetAsync(target);
            var muteDuration = ParseDuration(duration);

            if (target.GuildPermissions.Administrator)
            {
                await ReplyAsync(await EmbedHandler.CreateBasicEmbed("Moderation", $"Admins can't be muted.", Color.Red));
                return;
            }

            if (!target.IsMuted)
            {
                await user.MuteAsync(muteDuration, reason);
            }
            else await ReplyAsync(await EmbedHandler.CreateBasicEmbed("Moderation", $"User is already muted.", Color.Red));
        }

        [Command("Unmute")]
        [RequireUserPermission(GuildPermission.MuteMembers)]
        [RequireBotPermission(GuildPermission.MuteMembers)]
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
        [RequireUserPermission(GuildPermission.BanMembers)]
        [RequireBotPermission(GuildPermission.BanMembers)]
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
        [RequireUserPermission(GuildPermission.BanMembers)]
        [RequireBotPermission(GuildPermission.BanMembers)]
        [Summary("Display target user details")]
        public async Task Account(SocketGuildUser target = null, [Remainder] string args2 = "")
        {
            var guild = await Guilds.GetAsync(Context.Guild);
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
                    Users.ResetAsync(target);
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

            embed.AddField("In XP Cooldown", user.XP.InXPCooldown, inline: true);
            await ReplyAsync(embed.Build());
        }

        private static TimeSpan ParseDuration(string duration)
        {
            string letters = "";
            string numbers = "";

            foreach (char c in duration)
            {
                if (char.IsLetter(c))
                {
                    letters += c;
                }
                if (char.IsNumber(c))
                {
                    numbers += c;
                }
            }

            int time = int.Parse(numbers);

            switch (letters)
            {
                case string word when (word == "y" || word == "year"):
                    return TimeSpan.FromDays(365 * time);
                case string word when (word == "mo" || word == "month"):
                    return TimeSpan.FromDays(28 * time);
                case string word when (word == "w" || word == "week"):
                    return TimeSpan.FromDays(7 * time);
                case string word when (word == "d" || word == "day"):
                    return TimeSpan.FromDays(time);
                case string word when (word == "h" || word == "hour"):
                    return TimeSpan.FromHours(time);
                case string word when (word == "m" || word == "min"):
                    return TimeSpan.FromMinutes(time);
                case string word when (word == "s" || word == "sec"):
                    return TimeSpan.FromSeconds(time);
            }
            return TimeSpan.FromDays(7);
        }
    }
}