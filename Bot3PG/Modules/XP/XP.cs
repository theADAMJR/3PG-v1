using Bot3PG.Data;
using Bot3PG.Handlers;
using Bot3PG.Modules.General;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Linq;
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
                {
                    cardColour = Context.Guild.GetRole(role.Value)?.Color ?? Color.Default;
                    continue;
                }
            }

            var embed = new EmbedBuilder();
            embed.WithThumbnailUrl(target.GetAvatarUrl());
            embed.AddField("User", target.Mention, true);
            embed.AddField("EXP", user.XP.EXP, true);
            embed.AddField("EXP for Next Level", user.XP.EXPForNextLevel, true);
            embed.AddField("Level", user.XP.Level, true);
            embed.AddField("Rank", rank, false);
            embed.WithColor(cardColour);

            await ReplyAsync(embed);
        }

        [Command("Leaderboard")]
        [Summary("Display the user's with the highest EXP in a server")]
        [RequireUserPermission(GuildPermission.SendMessages)]
        public async Task Leaderboard(int page = 1)
        {
            var guild = await Guilds.GetAsync(Context.Guild);

            if (page < 1 || page > guild.XP.MaxLeaderboardPage)
            {
                await ReplyAsync(embed: await EmbedHandler.CreateBasicEmbed($"🏆 {Context.Guild.Name} Leaderboard", $"Leaderboard page must between 1 and {guild.XP.MaxLeaderboardPage}", Color.Red));
                return;
            }

            var usersPerPage = 10;
            var pageStartIndex = (page * usersPerPage) - usersPerPage;
            var leaderboardUsers = await Users.GetGuildUsersAsync(Context.Guild);
            leaderboardUsers = leaderboardUsers.OrderByDescending(u => u.XP.EXP).ToList();
            var pageEndIndex = page * usersPerPage;

            var embed = new EmbedBuilder();
            string leaderboard = "\u200B\n";
            for (int i = pageStartIndex; i < pageEndIndex; i++)
            {
                if (i >= leaderboardUsers.Count)
                {
                    leaderboard += $"**#{i + 1}** - N/A\n";
                    continue;
                }
                var user = leaderboardUsers[i];
                var socketGuildUser = Context.Guild.GetUser(user.ID);
                leaderboard += $"**#{i + 1}** - {user.XP.EXP} XP - {socketGuildUser?.Mention ?? "N/A"}\n";
            }
            embed.WithColor(Color.Teal);
            embed.AddField($"🏆 **{ Context.Guild.Name} Leaderboard **", leaderboard, inline: false);
            embed.AddField("View Leaderboard", $"{Global.Config.WebappLink}/servers/{Context.Guild.Id}/leaderboard");
            embed.WithThumbnailUrl(Context.Guild.IconUrl);
            embed.WithFooter($"Page {page}/{guild.XP.MaxLeaderboardPage} • Users with XP: {leaderboardUsers.Count}");

            await ReplyAsync(embed);
        }
    }
}