using Bot3PG.Handlers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Victoria;

namespace Bot3PG.CommandModules
{
    public class User : ModuleBase<SocketCommandContext>
    {
        public Lavalink Lavalink { get; set; }

        [Command("Help")]
        public async Task Help([Remainder]string args = "")
        {
            var target = Context.User;
            var prefix = Global.Config.CommandPrefix;
            
            if (args.ToLower() == "user")
            {
                await UserInfo(prefix, target);
            }
            else if (args.ToLower() == "xp" || args == null)
            {
                await XPInfo(prefix, target);
            }
            else if (args.ToLower() == "music" || args == null)
            {
                await MusicInfo(prefix, target);
            }
            else if (args.ToLower() == "moderation" || args == null)
            {
                await ModerationInfo(prefix, target);
            }
            else if (args.ToLower() == "admin" || args == null)
            {
                await AdminInfo(prefix, target);
            }
            else
            {
                await UserInfo(prefix, target);
                await XPInfo(prefix, target);
                await MusicInfo(prefix, target);
                await ModerationInfo(prefix, target);
                await AdminInfo(prefix, target);
            }
        }
        private async Task UserInfo(string prefix, SocketUser target)
        {
            var embed = new EmbedBuilder();
            embed.WithThumbnailUrl(Context.Client.CurrentUser.GetAvatarUrl());
            embed.WithTitle($"**{Context.Client.CurrentUser.Username} - 👥 User commands**");
            embed.AddField($"{prefix}help (user/xp/music/moderation/admin)", $"Display module specific or all commands", true);
            embed.AddField($"{prefix}ping", $"Test for bot response", true);
            embed.AddField($"{prefix}stats", "Display server statistics", true);
            embed.AddField($"{prefix}bot", "Display bot statistics", true);
            embed.AddField($"{prefix}embed (title, image, desc)", "Create an embed", true);
            embed.WithColor(Color.DarkGreen);
            await target.SendMessageAsync("", embed: embed.Build());
        }
        private async Task XPInfo(string prefix, SocketUser target)
        {
            var embed = new EmbedBuilder();
            embed.WithThumbnailUrl(Context.Client.CurrentUser.GetAvatarUrl());
            embed.WithTitle($"**{Context.Client.CurrentUser.Username} - ⭐ XP commands**");
            embed.AddField($"{prefix}xp (user)", "Display user XP", true);
            embed.AddField($"{prefix}leaderboard [page #]", "Display users with highest XP in server", true);
            embed.AddField($"{prefix}leaderboard global [page #]", $"Display users with highest XP globally", true);
            embed.WithColor(Color.DarkBlue);
            await target.SendMessageAsync("", embed: embed.Build());
        }
        private async Task MusicInfo(string prefix, SocketUser target)
        {
            var embed = new EmbedBuilder();
            embed.WithThumbnailUrl(Context.Client.CurrentUser.GetAvatarUrl());
            embed.WithTitle($"**{Context.Client.CurrentUser.Username} - 🎶 [ALPHA] Music commands**");
            embed.AddField($"{prefix}join", "Get bot to join your voice channel", true);
            embed.AddField($"{prefix}leave", "Get bot to leave your voice channel", true);
            embed.AddField($"{prefix}play (query)", "Search YouTube for tracks to play", true);
            embed.AddField($"{prefix}stop", "Stop player", true);
            embed.AddField($"{prefix}list", "List track queue", true);
            embed.AddField($"{prefix}skip", "Skip current track", true);
            embed.AddField($"{prefix}volume (value)", "Set player volume", true);
            embed.AddField($"{prefix}pause", "Pause player", true);
            embed.AddField($"{prefix}resume", "Resume player if paused", true);
            embed.WithColor(Color.Blue);
            await target.SendMessageAsync("", embed: embed.Build());
        }
        private async Task ModerationInfo(string prefix, SocketUser target)
        {
            var embed = new EmbedBuilder();
            embed.WithThumbnailUrl(Context.Client.CurrentUser.GetAvatarUrl());
            embed.WithTitle($"**{Context.Client.CurrentUser.Username} - 🛡️ Moderation commands**");
            embed.AddField($"{prefix}warn (user, reason)", "Warn user [with reason]", true);
            embed.AddField($"{prefix}kick (user, reason)", "Kick user [with reason]", true);
            embed.AddField($"{prefix}mute (user, reason)", "Mute user [with reason]", true);
            embed.AddField($"{prefix}unmute (user, reason)", "Unmute user [with reason]", true);
            embed.AddField($"{prefix}ban (user, reason)", "Ban user [with reason]", true);
            embed.AddField($"{prefix}unban (user, reason)", "Ban user [with reason]", true);
            embed.WithColor(Color.Orange);
            await target.SendMessageAsync("", embed: embed.Build());
        }
        private async Task AdminInfo(string prefix, SocketUser target)
        {
            var embed = new EmbedBuilder();
            embed.WithThumbnailUrl(Context.Client.CurrentUser.GetAvatarUrl());
            embed.WithTitle($"**{Context.Client.CurrentUser.Username} - 👑 Admin commands**");
            embed.AddField($"{prefix}say (message)", "Get bot to send message", true);
            embed.AddField($"{prefix}image (URL)", "Get bot to send image", true);
            embed.AddField($"{prefix}rulebox", "Get bot to send rule agreement box", true);
            embed.WithColor(Color.Purple);
            await target.SendMessageAsync("", embed: embed.Build());
        }

        [Command("Ping")]
        public async Task Ping()
        {
            var embed = await EmbedHandler.CreateSimpleEmbed("Pong!", "🏓", Color.Magenta);
            await ReplyAsync("", embed: embed);
        }

        [Command("Bot")]
        public async Task Bot()
        {
            string creationDate = $"{Context.Client.CurrentUser.CreatedAt.Day}/{Context.Client.CurrentUser.CreatedAt.Month}/{Context.Client.CurrentUser.CreatedAt.Year}";

            var embed = new EmbedBuilder();
            embed.WithTitle($"{Context.Client.CurrentUser.Username} stats");
            embed.AddField("Creator", Context.Client.CurrentUser, true);
            embed.AddField("Servers", Context.Client.Guilds.Count, true);
            embed.AddField("Uptime", $"{Global.Uptime.Days}d {Global.Uptime.Hours}h {Global.Uptime.Minutes}m {Global.Uptime.Seconds}s", true);
            embed.AddField("Creation Date", creationDate, true);
            embed.AddField("DM Channels", Context.Client.DMChannels.Count, true);
            //embed.AddField("Processor Count", Environment.ProcessorCount, true);
            //embed.AddField("CPU Usage", Context.Guild.TextChannels.Count, true);
            //embed.AddField("Memory Usage", Context.Guild.TextChannels.Count, true);
            embed.WithThumbnailUrl(Context.Client.CurrentUser.GetAvatarUrl());
            embed.WithFooter($"Bot ID: {Context.Client.CurrentUser.Id}");
            embed.WithColor(Color.DarkMagenta);

            await ReplyAsync("", embed: embed.Build());
        }

        [Command("Stats")]
        public async Task Stats()
        {
            string creationDate = $"{Context.Guild.CreatedAt.Day}/{Context.Guild.CreatedAt.Month}/{Context.Guild.CreatedAt.Year}";

            var embed = new EmbedBuilder();
            embed.WithThumbnailUrl(Context.Guild.IconUrl);
            embed.WithColor(Color.DarkMagenta);
            embed.AddField("Owner", Context.Guild.Owner.Mention, true);
            embed.AddField("Default Channel", Context.Guild.DefaultChannel.Mention, true);
            embed.AddField("Member Count", Context.Guild.MemberCount, true);
            embed.AddField("Creation Date", creationDate, true);
            embed.AddField("Role Count", Context.Guild.Roles.Count, true);
            embed.AddField("Channel Count", Context.Guild.Channels.Count, true);
            embed.AddField("Text Channels", Context.Guild.TextChannels.Count, true);
            embed.AddField("Voice Channels", Context.Guild.VoiceChannels.Count, true);
            embed.WithFooter($"Server Name: {Context.Guild.Name} | ServerID: {Context.Guild.Id}");

            await ReplyAsync("", embed: embed.Build());
        }

        [Command("Embed")]
        public async Task Embed(string title = "Not set", string imgUrl = "", [Remainder]string desc = "Not set")
        {
            var embed = new EmbedBuilder();
            embed.WithTitle(title);
            embed.WithDescription(desc);
            embed.WithColor(Color.DarkGreen);

            if (imgUrl.Contains("http") || imgUrl.Contains("data"))
            {
                embed.WithThumbnailUrl(imgUrl);
            }
            await ReplyAsync("", embed: embed.Build());
        }
    }
}