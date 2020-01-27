using Bot3PG.Data;
using Bot3PG.Data.Structs;
using Bot3PG.Handlers;
using Bot3PG.Utils;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using System.Linq;
using Discord.Rest;
using System.Collections.Generic;
using System;

namespace Bot3PG.Modules.Moderation
{
    [Color(75, 65, 150)]
    public sealed class Moderation : CommandBase
    {
        internal override string ModuleName => "Moderation 🔨";
        internal override Color ModuleColour => Color.Orange;

        [Command("Kick")]
        [Summary("Kick a user [with reason]")]
        [RequireUserPermission(GuildPermission.KickMembers), RequireBotPermission(GuildPermission.KickMembers)]
        public async Task KickUser(SocketGuildUser target, [Remainder] string reason = "No reason provided.")
        {
            try
            {
                var user = await Users.GetAsync(target);
                if (target.GuildPermissions.Administrator)
                    throw new InvalidOperationException($"Admins can't be kicked.");
                
                await user.KickAsync(reason, Context.User);
                await ReplyAsync(await EmbedHandler.CreateBasicEmbed(ModuleName, $"Kicked {target.Mention} - `{reason}`.", Color.Orange));                
            }
            catch (InvalidOperationException ex) { await ReplyAsync(EmbedHandler.CreateErrorEmbed(ModuleName, ex.Message)); }
        }

        [Command("Ban")]
        [Summary("Ban a user [with reason]")]
        [RequireUserPermission(GuildPermission.BanMembers), RequireBotPermission(GuildPermission.BanMembers)]
        [Remarks("**Accepted Values:** s/sec m/min h/hour d/day w/week mo/month y/year")]
        public async Task BanUser(SocketGuildUser target, string duration = "1d", [Remainder] string reason = "No reason provided.")
        {
            try
            {
                var user = await Users.GetAsync(target);
                var banDuration = CommandUtils.ParseDuration(duration);

                if (user.Status.IsBanned)
                    throw new InvalidOperationException($"User is banned");

                var targetHighest = target.Hierarchy;
                var senderHighest = (Context.Message.Author as SocketGuildUser).Hierarchy;

                if (targetHighest >= senderHighest)
                    throw new InvalidOperationException($"Higher rank user cannot be banned");
                else
                {
                    await user.BanAsync(banDuration, reason, Context.User);
                    await ReplyAsync(await EmbedHandler.CreateBasicEmbed(ModuleName, $"Banned {target.Mention} for `{duration}` - `{reason}`.", Color.Orange));
                }
            }
            catch (InvalidOperationException ex) { await ReplyAsync(EmbedHandler.CreateErrorEmbed(ModuleName, ex.Message)); }
        }

        [Command("Unban")]
        [Summary("Unban a user [with reason]")]
        [RequireUserPermission(GuildPermission.BanMembers), RequireBotPermission(GuildPermission.BanMembers)]
        public async Task UnbanUser(ulong targetId, [Remainder] string reason = "No reason provided.")
        {
            try
            {
                RestBan restBan = null;
                try { restBan = await Context.Guild.GetBanAsync(targetId); }
                catch { throw new InvalidOperationException($"User is not banned"); }

                await Context.Guild.RemoveBanAsync(targetId);
                await ReplyAsync(await EmbedHandler.CreateBasicEmbed(ModuleName, $"Unbanned {restBan.User.Mention} - `{reason}`.", Color.Orange));
                if (!restBan.User.IsBot)
                    await restBan.User.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed(ModuleName, 
                        $"You have been unbanned from {Context.Guild.Name} for '{reason}'", Color.Green));
            }
            catch (InvalidOperationException ex) { await ReplyAsync(EmbedHandler.CreateErrorEmbed(ModuleName, ex.Message)); }
        }

        [Command("Mute")]
        [RequireUserPermission(GuildPermission.MuteMembers), RequireBotPermission(GuildPermission.MuteMembers)]
        [Summary("Mute a user's voice and chat [with reason]")]
        public async Task MuteUser(SocketGuildUser target, string duration = "1d", [Remainder] string reason = "No reason provided.")
        {
            try
            {
                var user = await Users.GetAsync(target) ?? new GuildUser(target);
                var muteDuration = CommandUtils.ParseDuration(duration);

                if (target.GuildPermissions.Administrator)
                    throw new InvalidOperationException($"Admins cannot be muted");

                if (!user.Status.IsMuted)
                    throw new InvalidOperationException($"User is not muted");

                await user.MuteAsync(muteDuration, reason, Context.User);
                await ReplyAsync(await EmbedHandler.CreateBasicEmbed(ModuleName, $"Muted {target.Mention} for `{duration}` - `{reason}`", Color.Green));
            }
            catch (InvalidOperationException ex) { await ReplyAsync(EmbedHandler.CreateErrorEmbed(ModuleName, ex.Message)); }
        }

        [Command("Unmute")]
        [RequireUserPermission(GuildPermission.MuteMembers), RequireBotPermission(GuildPermission.MuteMembers)]
        [Summary("Unmute a user's voice and chat [with reason]")]
        public async Task UnmuteUser(SocketGuildUser target, [Remainder] string reason = "No reason provided.")
        {
            try
            {
                var user = await Users.GetAsync(target);
                if (!user.Status.IsMuted)
                    throw new InvalidOperationException($"User is not muted");

                await user.UnmuteAsync(reason, Context.User);
                await ReplyAsync(await EmbedHandler.CreateBasicEmbed(ModuleName, $"Unmuted {target.Mention} - `{reason}`.", Color.Orange));
            }
            catch (InvalidOperationException ex) { await ReplyAsync(EmbedHandler.CreateErrorEmbed(ModuleName, ex.Message)); }
        }

        [Command("Warn")]
        [RequireUserPermission(GuildPermission.BanMembers), RequireBotPermission(GuildPermission.BanMembers)]
        [Summary("Warn a user [with reason] and add a warning to their account")]
        public async Task WarnUser(SocketGuildUser target, [Remainder] string reason = "No reason provided.")
        {
            try
            {
                if (target.IsBot)
                    throw new InvalidOperationException($"Bots cannot be warned");

                var user = await Users.GetAsync(target);
                if (target.GuildPermissions.Administrator)
                    throw new InvalidOperationException($"Admins can't be warned");

                await user.WarnAsync(reason, Context.User);
                await ReplyAsync(await EmbedHandler.CreateBasicEmbed(ModuleName, $"Warned {target.Mention} - `{reason}`.", Color.Orange));                
            }
            catch (InvalidOperationException ex) { await ReplyAsync(EmbedHandler.CreateErrorEmbed(ModuleName, ex.Message)); }
        }

        [Command("User")]
        [RequireUserPermission(GuildPermission.BanMembers), RequireBotPermission(GuildPermission.BanMembers)]
        [Summary("Display target user details"), Remarks("Actions: reset")]
        public async Task User(SocketGuildUser target = null, [Remainder] string action = "")
        {
            try
            {
                target ??= Context.User as SocketGuildUser;

                var user = await Users.GetAsync(target);
                if (action == "reset")
                    await ResetUser(target);

                var embed = new EmbedBuilder()
                    .WithThumbnailUrl(target.GetAvatarUrl())
                    .WithColor(Color.Orange)
                    .WithTitle($"**{target.Username}**")
                    .AddField("Warnings", user.Status.WarningsCount, inline: true);

                embed.AddField("Is Banned", user.Status.IsBanned, inline: true);
                if (user.Status.IsBanned)
                {
                    var ban = user.Status.Bans.Last();
                    embed.AddField("Ban Reason", ban.Reason, inline: true);
                    embed.AddField("Start of Ban", ban.Start, inline: true);
                    embed.AddField("End of Ban", ban.End, inline: true);
                }

                embed.AddField("Is Muted", user.Status.IsMuted, inline: true);
                if (user.Status.IsMuted)
                {
                    var mute = user.Status.Mutes.Last();
                    embed.AddField("Mute Reason", mute.Reason, inline: true);
                    embed.AddField("Start of Mute", mute.Start, inline: true);
                    embed.AddField("End of Mute", mute.End, inline: true);
                }

                var userInCooldown = await user.XP.GetXPCooldown();
                embed.AddField("In XP Cooldown", userInCooldown, inline: true);
                await ReplyAsync(embed);                
            }
            catch (ArgumentException ex) { await ReplyAsync(EmbedHandler.CreateErrorEmbed(ModuleName, ex.Message)); }
        }

        private async Task ResetUser(SocketGuildUser target)
        {
            if (target is null)
                throw new InvalidOperationException("User not found");
            else
            {
                await Users.ResetAsync(target);
                await ReplyAsync(await EmbedHandler.CreateBasicEmbed(ModuleName, "User account reset", Color.Orange));
                return;
            }
        }

        [Command("Clear"), Alias("Purge")]
        [RequireUserPermission(GuildPermission.ManageMessages), RequireBotPermission(GuildPermission.ManageMessages)]
        [Summary("Remove a specified amount of messages from a channel")]
        public async Task ClearMessages(int amount = 100)
        {
            try
            {
                const int max = 100;
                if (amount <= 0 || amount > max)
                    throw new ArgumentException("Messages to remove must be between 1 and 100");
                
                var messages = amount == -1 ? Context.Channel.GetMessagesAsync().FlattenAsync().Result : Context.Channel.GetMessagesAsync(amount).FlattenAsync().Result;
                var channel = Context.Channel as SocketTextChannel;

                try { await channel.DeleteMessagesAsync(messages); }
                catch (Exception ex) { throw new InvalidOperationException(ex.Message); }

                var reply = await ReplyAsync(await EmbedHandler.CreateSimpleEmbed("Clear", $"Cleared `{messages.Count()}` messages from {channel.Mention}", Color.Blue));

                await Task.Delay(4000);
                await reply.DeleteAsync();
            }
            catch (ArgumentException ex) { await ReplyAsync(EmbedHandler.CreateErrorEmbed(ModuleName, ex.Message)); }
            catch (InvalidOperationException ex) { await ReplyAsync(EmbedHandler.CreateErrorEmbed(ModuleName, ex.Message)); }
        }

        [Command("Freeze"), Alias("Lock")]
        [RequireUserPermission(GuildPermission.ManageChannels), RequireBotPermission(GuildPermission.ManageChannels)]
        [Summary("Prevent messages/reactions in a channel")]
        public async Task FreezeChannel()
        {
            var textChannel = Context.Channel as SocketTextChannel;            
            await textChannel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, OverwritePermissions.DenyAll(Context.Channel));
            await ReplyAsync(await EmbedHandler.CreateSimpleEmbed(ModuleName, $"🔒 Locked {textChannel.Mention}", ModuleColour));
        }

        [Command("Unfreeze"), Alias("Unlock")]
        [RequireUserPermission(GuildPermission.ManageChannels), RequireBotPermission(GuildPermission.ManageChannels)]
        [Summary("Prevent messages/reactions in a channel")]
        public async Task UnfreezeChannel()
        {
            var textChannel = Context.Channel as SocketTextChannel;            
            await textChannel.RemovePermissionOverwriteAsync(Context.Guild.EveryoneRole);
            await ReplyAsync(await EmbedHandler.CreateSimpleEmbed(ModuleName, $"🔓 Unlocked {textChannel.Mention}", ModuleColour));
        }
    }
}