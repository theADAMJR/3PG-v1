using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Bot3PG.Handlers;
using Bot3PG.Core.Data;
using System;

namespace Bot3PG.Modules.XP
{
    public sealed class XP : CommandBase
    {
        [Command("XP"), Alias("EXP", "Rank")]
        [Summary("Display a user's XP stats")]
        [RequireUserPermission(GuildPermission.SendMessages)]
        public async Task MyStats()
        {
            var mentionedUser = Context.Message.MentionedUsers.FirstOrDefault();
            var target = mentionedUser as SocketGuildUser ?? Context.User as SocketGuildUser;

            var guild = await Guilds.GetAsync(Context.Guild);
            var users = Users.GetLeaderboardUsers(Context.Guild);

            int rank = 1;// users.FindIndex(u => target.Id == u.ID) + 1;
            var user = await Users.GetAsync(target);

            // TODO - add config for level boundaries
            var cardColor = Color.DarkGrey;
            switch (user.XP.LevelNumber)
            {
                case uint level when (level >= 25 && level < 50):
                    cardColor = Color.Red;
                    break;
                case uint level when (level >= 50 && level < 75):
                    cardColor = Color.LightGrey;
                    break;
                case uint level when (level >= 75 && level < 100):
                    cardColor = Color.Gold;
                    break;
                case uint level when (level >= 100):
                    cardColor = Color.Blue;
                    break;
            }

            var embed = new EmbedBuilder();
            embed.WithThumbnailUrl(target.GetAvatarUrl());
            embed.AddField("User", target.Mention, true);
            embed.AddField("XP", user.XP.EXP, true);
            embed.AddField("Level", user.XP.LevelNumber, true);
            embed.AddField("Rank", rank, false);
            //embed.AddField("XP for Next Level", user.XP.EXP);
            // TODO add XP until next rank
            embed.WithColor(cardColor);

            await ReplyAsync(embed);
        }

        /*[Command("Leaderboard")]
        [Summary("Display the user's with the highest EXP in a server")]
        [RequireUserPermission(GuildPermission.SendMessages)]
        public async Task Leaderboard([Remainder] string args = "1")
        {
            var guild = await Guilds.GetAsync(Context.Guild);
            var usersPerPage = 10;

            int page = Convert.ToInt32(args) == 0 ? 1 : Convert.ToInt32(args);
            var type = page > 0 ? "xp" : args;

            switch (type)
            {
                case "date joined":
                    System.Console.WriteLine("date joined");
                    return;
                default:
                    break;
            }

            if (page < 1 || page > guild.Config.MaxLeaderboardPage)
            {
                await ReplyAsync("", embed: await EmbedHandler.CreateBasicEmbed($"🏆 {Context.Guild.Name} Leaderboard", $"Leaderboard page must between 1 and {guild.Config.MaxLeaderboardPage}", Color.Red));
                return;
            }

            var pageStartIndex = (page * usersPerPage) - usersPerPage;
            var leaderboardUsers = Users.GetLeaderboardUsers(Context.Guild);
            var pageEndIndex = page * usersPerPage;

            var embed = new EmbedBuilder();
            for (int i = pageStartIndex; i < pageEndIndex; i++)
            {
                if (i >= leaderboardUsers.Count) continue;
                var socketGuildUser = Context.User as SocketGuildUser;//Context.Guild.GetUser(leaderboardUsers[i].ID);
                embed.AddField($"#{i + 1} - {await Users.GetAsync(socketGuildUser).XP.EXP} XP", $"{socketGuildUser.Mention}", false);
            }
            embed.WithTitle($"🏆 **{Context.Guild.Name} Leaderboard**");
            embed.WithColor(Color.Teal);
            embed.WithThumbnailUrl(Context.Guild.IconUrl);
            embed.WithFooter($"Page {page} • Users with XP: {leaderboardUsers.Count}");

            await ReplyAsync(embed);
        }*/
    }
}