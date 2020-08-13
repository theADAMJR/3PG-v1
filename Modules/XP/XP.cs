using Bot3PG.Data;
using Bot3PG.Handlers;
using Bot3PG.Modules.General;
using Bot3PG.Utils;
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
            
            string imageURL = $"{Global.Config.DashboardURL}/api/servers/{target.Guild.Id}/users/{target.Id}/xp-card";
            System.Console.WriteLine(imageURL);
            var stream = await CommandUtils.DownloadData(imageURL);
            await Context.Channel.SendFileAsync(stream, "server-xp-card.png");
        }

        [Command("XP"), Alias("EXP", "Rank")]
        [Summary("Display a user's XP stats")]
        [RequireUserPermission(GuildPermission.SendMessages)]
        public async Task ShowRankEXP(int rank)
        {
            try
            {
                if (rank <= 0)
                    throw new ArgumentException("Rank cannot be less than 0");

                var rankedUsers = await GuildUsers.GetRankedGuildUsersAsync(Context.Guild);
                if (rank > rankedUsers.Count)
                    throw new ArgumentException("Rank exceeds number of ranked users");

                var target = rankedUsers[rank - 1];
                if (target is null)
                    throw new InvalidOperationException($"User at rank `{rank}` could not be found");

                string imageURL = $"{Global.Config.DashboardURL}/api/servers/{target.Guild.Id}/users/{target.Id}/xp-card";
                var stream = await CommandUtils.DownloadData(imageURL);
                await Context.Channel.SendFileAsync(stream, "server-xp-card.png");                
            }
            catch (ArgumentException ex) { await ReplyAsync(EmbedHandler.CreateErrorEmbed(ModuleName, ex.Message)); }
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

                var users = await GuildUsers.GetGuildUsersAsync(Context.Guild);
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
                embed.AddField("View Leaderboard", $"{Global.Config.DashboardURL}/servers/{Context.Guild.Id}/leaderboard");
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
            
            string imageURL = $"{Global.Config.DashboardURL}/api/users/{target.Id}/xp-card";
            var stream = await CommandUtils.DownloadData(imageURL);
            await Context.Channel.SendFileAsync(stream, "xp-card.png");
        }
    }
}