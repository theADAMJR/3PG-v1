using Bot3PG.Data;
using Bot3PG.Handlers;
using Bot3PG.Modules.General;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Bot3PG.Modules.XP
{
    [Color(75, 40, 65)]
    public sealed class XP : CommandBase
    {
        internal override string ModuleName => "XP ✨";
        internal override Color ModuleColour => Color.Green;

        [Command("XP"), Alias("EXP", "Rank")]
        [Summary("Display a user's XP stats")]
        [RequireUserPermission(GuildPermission.SendMessages)]
        public async Task ShowEXP(SocketGuildUser target = null)
        {
            target ??= Context.User as SocketGuildUser;

            var guild = await Guilds.GetAsync(Context.Guild);
            var users = await Users.GetGuildUsersAsync(Context.Guild);
            users = users.OrderByDescending(u => u.XP.EXP).ToList();

            int rank = users.FindIndex(u => u.ID == target.Id) + 1;
            var user = await Users.GetAsync(target);

            var cardColour = Color.DarkGrey;
            var roles = guild.XP.RoleRewards.LevelRoles.OrderBy(r => r.Key);

            foreach (var role in roles)
            {
                int.TryParse(role.Key, out int boundary);
                if (user.XP.Level >= boundary)
                    cardColour = Context.Guild.GetRole(role.Value)?.Color ?? Color.Default;
            }

            var embed = new EmbedBuilder();
            embed.WithThumbnailUrl(target.GetAvatarUrl());
            embed.AddField("User", target.Mention, true);
            embed.AddField("EXP", user.XP.EXP, true);
            embed.AddField("EXP for Next Level", user.XP.EXPForNextLevel, true);
            embed.AddField("Level", user.XP.Level, true);
            embed.AddField("Rank", $"#{rank}", false);
            embed.WithColor(cardColour);

            await ReplyAsync(embed);
        }

        [Command("Leaderboard")]
        [Summary("Display the users with the highest EXP in a server")]
        [RequireUserPermission(GuildPermission.SendMessages)]
        public async Task Leaderboard(int page = 1)
        {
            try
            {
                var guild = await Guilds.GetAsync(Context.Guild);

                if (page < 1 || page > guild.XP.MaxLeaderboardPage)
                    throw new ArgumentException($"Leaderboard page must between 1 and {guild.XP.MaxLeaderboardPage}");
                
                int usersPerPage = 10;
                int pageStartIndex = (page * usersPerPage) - usersPerPage;
                int pageEndIndex = page * usersPerPage;

                var users = await Users.GetGuildUsersAsync(Context.Guild);
                users = users.OrderByDescending(u => u.XP.EXP).ToList();

                string details = "\n";
                for (int i = pageStartIndex; i < pageEndIndex; i++)
                {
                    if (i >= users.Count)
                    {
                        details += $"**#{i + 1}** - N/A\n";
                        continue;
                    }
                    var user = users[i];
                    var socketGuildUser = Context.Guild.GetUser(user.ID);
                    details += $"**#{i + 1}** - {user.XP.EXP} XP - {socketGuildUser?.Mention ?? "N/A"}\n";
                }

                var embed = new EmbedBuilder();
                embed.WithColor(Color.Teal);
                embed.AddField($"🏆 **{ Context.Guild.Name} Leaderboard **", details, inline: false);
                embed.AddField("View Leaderboard", $"{Global.Config.WebappLink}/servers/{Context.Guild.Id}/leaderboard");
                embed.WithThumbnailUrl(Context.Guild.IconUrl);
                embed.WithFooter($"Page {page}/{guild.XP.MaxLeaderboardPage} • Users with XP: {users.Count}");

                await ReplyAsync(embed);
            }
            catch (ArgumentException ex) { await ReplyAsync(EmbedHandler.CreateErrorEmbed(ModuleName, ex.Message)); }
        }

        [Command("Profile"), Alias("GXP")]
        [Summary("Display a user's global XP card")]
        [RequireUserPermission(GuildPermission.SendMessages)]
        public async Task GlobalProfile(SocketUser target = null)
        {
            target ??= Context.User;
            string imageURL = $"{Global.Config.WebappLink}/api/users/{target.Id}/xp-card";

            var stream = DownloadData(imageURL);
            await Context.Channel.SendFileAsync(stream, "xp-card.png");
        }

        private Stream DownloadData(string url)
        {
            try
            {
                var req = WebRequest.Create(url);
                var res = req.GetResponse();
                return res.GetResponseStream();
            }
            catch (Exception) { throw new Exception("There was a problem downloading the image file."); }
        }
    }
}