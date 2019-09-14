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
            var users = await Users.GetGuildUsersAsync(Context.Guild);
            users = users.OrderByDescending(user => user.XP.EXP).ToList();

            int rank = users.FindIndex(user => user.ID == target.Id) + 1;
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
            embed.AddField("EXP", user.XP.EXP, true);
            embed.AddField("EXP for Next Level", user.XP.EXPForNextLevel, true);
            embed.AddField("Level", user.XP.LevelNumber, true);
            embed.AddField("Rank", rank, false);
            //embed.AddField("XP for Next Level", user.XP.EXP);
            // TODO add XP until next rank
            embed.WithColor(cardColor);

            await ReplyAsync(embed);
        }

        [Command("Leaderboard")]
        [Summary("Display the user's with the highest EXP in a server")]
        [RequireUserPermission(GuildPermission.SendMessages)]
        public async Task Leaderboard(int page = 1)
        {
            try
            {
                var guild = await Guilds.GetAsync(Context.Guild);

                if (page < 1 || page > guild.XP.MaxLeaderboardPage)
                {
                    await ReplyAsync("", embed: await EmbedHandler.CreateBasicEmbed($"🏆 {Context.Guild.Name} Leaderboard", $"Leaderboard page must between 1 and {guild.XP.MaxLeaderboardPage}", Color.Red));
                    return;
                }

                var usersPerPage = 10;
                var pageStartIndex = (page * usersPerPage) - usersPerPage;
                var leaderboardUsers = await Users.GetGuildUsersAsync(Context.Guild);
                leaderboardUsers = leaderboardUsers.OrderByDescending(u => u.XP.EXP).ToList();
                var pageEndIndex = page * usersPerPage;

                var embed = new EmbedBuilder();
                for (int i = pageStartIndex; i < pageEndIndex; i++)
                {
                    if (i >= leaderboardUsers.Count)
                    {
                        embed.AddField($"#{i + 1} -  XP", $"-", inline: false);
                        continue;
                    }
                    var user = leaderboardUsers[i];
                    var socketGuildUser = Context.Guild.GetUser(user.ID);
                    embed.AddField($"#{i + 1} - {user.XP.EXP} XP", $"{socketGuildUser.Mention}", inline: false);
                }
                embed.WithTitle($"🏆 **{Context.Guild.Name} Leaderboard**");
                embed.WithColor(Color.Teal);
                embed.WithThumbnailUrl(Context.Guild.IconUrl);
                embed.WithFooter($"Page {page} • Users with XP: {leaderboardUsers.Count}");

                await ReplyAsync(embed);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}