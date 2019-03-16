using Bot3PG.Core.LevelingSystem;
using Bot3PG.Core.Users;
using Bot3PG.DataStructs;
using Bot3PG.Handlers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Bot3PG.Services;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace Bot3PG.CommandModules
{
    public class Moderation : ModuleBase<SocketCommandContext>
    {
        [Command("Kick")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        [RequireBotPermission(GuildPermission.KickMembers)]
        public async Task KickUser(IGuildUser user, string reason = "No reason provided.")
        {
            if (user.GuildPermissions.Administrator)
            {
                await ReplyAsync("", embed: await EmbedHandler.CreateBasicEmbed("Kick", $"Admins can't be kicked.", Color.Red));
                return;
            }
            await user.KickAsync(reason);
            await user.SendMessageAsync($"You have been kicked from {Context.Guild.Name} - '{reason}'");
        }

        [Command("Ban")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        [RequireBotPermission(GuildPermission.BanMembers)]
        public async Task BanUser(IGuildUser user, [Remainder] string reason = "No reason provided.")
        {
            if (user.GuildPermissions.Administrator)
            {
                await ReplyAsync("", embed: await EmbedHandler.CreateBasicEmbed("Ban", $"Admins can't be banned.", Color.Red));
                return;
            }
            Console.WriteLine(Accounts.GetAccount(user as SocketGuildUser).IsBanned);
            Accounts.GetAccount(user as SocketGuildUser).IsBanned = true;
            Console.WriteLine(Accounts.GetAccount(user as SocketGuildUser).IsBanned);
            await user.SendMessageAsync($"You have been banned from {Context.Guild.Name} - '{reason}'");
            await user.Guild.AddBanAsync(user, 0, reason); // TODO - add prune option
        }

        [Command("Unban")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        [RequireBotPermission(GuildPermission.BanMembers)]
        public async Task UnbanUser(IGuildUser user, [Remainder]string reason = "No reason provided.")
        {
            var account = Accounts.GetAccountInGuild(user as SocketGuildUser);
            if (account == null)
            {
                await ReplyAsync("", embed: await EmbedHandler.CreateBasicEmbed("Unban", $"User account not found.", Color.Red));
                return;
            }
            Console.WriteLine(Accounts.GetAccount(user as SocketGuildUser).IsBanned);
            Accounts.GetAccount(user as SocketGuildUser).IsBanned = false;
            Console.WriteLine(Accounts.GetAccount(user as SocketGuildUser).IsBanned);
            await Context.Guild.RemoveBanAsync(account.ID);
            await user.GetOrCreateDMChannelAsync();
            await user.SendMessageAsync($"You have been unbanned from {Context.Guild.Name} - '{reason}'");
        }

        [Command("Mute")]
        [RequireUserPermission(GuildPermission.MuteMembers)]
        [RequireBotPermission(GuildPermission.MuteMembers)]
        public async Task MuteUser(IGuildUser user, string reason = "No reason provided.")
        {
            var account = Accounts.GetAccount(user as SocketGuildUser);

            if (user.GuildPermissions.Administrator)
            {
                await ReplyAsync("", embed: await EmbedHandler.CreateBasicEmbed("Mute", $"Admins can't be muted.", Color.Red));
                return;
            }

            if (!user.IsMuted)
            {
                account.IsMuted = true;
                await user.ModifyAsync(x => x.Mute = true);
                await user.GetOrCreateDMChannelAsync();
            }
            else await ReplyAsync("", embed: await EmbedHandler.CreateBasicEmbed("Unmute", $"User is already muted.", Color.Red));
        }

        [Command("Unmute")]
        [RequireUserPermission(GuildPermission.MuteMembers)]
        [RequireBotPermission(GuildPermission.MuteMembers)]
        public async Task UnmuteUser(IGuildUser user, string reason = "No reason provided.")
        {
            var account = Accounts.GetAccount(user as SocketGuildUser);

            if (user.IsMuted)
            {
                account.IsMuted = false;
                await user.ModifyAsync(x => x.Mute = false);
                await user.GetOrCreateDMChannelAsync();
            }
            else await ReplyAsync("", embed: await EmbedHandler.CreateBasicEmbed("Unmute", $"User account not found.", Color.Red));
        }

        [Command("Warn")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        [RequireBotPermission(GuildPermission.BanMembers)]
        public async Task WarnUser(IGuildUser user, string reason = "No reason provided.")
        {
            if (user.GuildPermissions.Administrator)
            {
                await ReplyAsync("", embed: await EmbedHandler.CreateBasicEmbed("Warn", $"Admins can't be warned.", Color.Red));
                return;
            }

            Accounts.GetAccount(user as SocketGuildUser).NumberOfWarnings++;
            Accounts.SaveAccounts();

        }

        [Command("Account")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        [RequireBotPermission(GuildPermission.KickMembers)]
        public async Task Account(string args1 = "", [Remainder]string args2 = "")
        {
            var mentionedUser = Context.Message.MentionedUsers.FirstOrDefault();
            var target = mentionedUser ?? Context.User;

            if (target.IsBot)
            {
                await ReplyAsync("", embed: await EmbedHandler.CreateBasicEmbed("Accounts", "Bots don't have accounts!", Color.Red));
                return;
            }
            var account = Accounts.GetAccount(target as SocketGuildUser);
            if (account == null)
            {
                await ReplyAsync("", embed: await EmbedHandler.CreateBasicEmbed("Accounts", "Account not found. Type /help for account options.", Color.Red));
                return;
            }
            if (args2 == "reset")
            {
                if (mentionedUser != null)
                {
                    Accounts.ResetUserAccount(mentionedUser as SocketGuildUser);
                    Accounts.SaveAccounts();
                    await ReplyAsync("", embed: await EmbedHandler.CreateBasicEmbed("Accounts", "User account reset", Color.Orange));
                    return;
                }
                else
                {
                    Accounts.ResetUserAccount(mentionedUser as SocketGuildUser);
                    await ReplyAsync("", embed: await EmbedHandler.CreateBasicEmbed("Accounts", "User not found", Color.Red));
                    return;
                }
            }

            var embed = new EmbedBuilder();
            embed.WithThumbnailUrl(target.GetAvatarUrl());
            embed.AddField("User", target.Mention, inline: true);
            embed.AddField("Agreed To Rules", account.AgreedToRules, inline: true);
            embed.AddField("Warnings", account.NumberOfWarnings, inline: true);
            embed.AddField("Is Banned", account.IsBanned, inline: true);
            embed.AddField("Is Muted", account.IsMuted, inline: true);
            embed.AddField("In XP Cooldown", Leveling.XPCooldownActive(account), inline: true);
            embed.AddField("Account Created", $"{target.CreatedAt.Day}/{target.CreatedAt.Month}/{target.CreatedAt.Year}", false);
            embed.WithColor(Color.Orange);

            await ReplyAsync("", embed: embed.Build());
        }
    }
}