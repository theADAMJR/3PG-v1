using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Audio;
using Discord.WebSocket;
using Bot3PG.Core.Users;
using System.Net;
using Newtonsoft.Json;
using Discord.Rest;
using Bot3PG.Handlers;

namespace Bot3PG.CommandModules
{
    public class XP : ModuleBase<SocketCommandContext>
    {
        [Command("XP")]
        public async Task MyStats([Remainder]string args = "")
        {
            SocketUser target = null;
            var mentionedUser = Context.Message.MentionedUsers.FirstOrDefault();
            target = mentionedUser ?? Context.User;

            // get user rank
            var userAccounts = Accounts.GetAccounts();
            var users = userAccounts
                .OrderByDescending(acc => acc.XP)
                .Take(10)
                .Select(u => GetUsernameById(u.ID, Context.Client.GetGuild(u.GuildID)))
                .ToList();

            int rank = users.FindIndex(a => target.Id == a.Id) + 1;

            var account = Accounts.GetAccount(target as SocketGuildUser);

            var cardColor = Color.DarkGrey; // set default colour

            if (account.LevelNumber >= 25 && account.LevelNumber < 50) // bronze
            {
                cardColor = Color.DarkOrange;
            }
            else if (account.LevelNumber >= 50 && account.LevelNumber < 75) // silver
            {
                cardColor = Color.LightGrey;
            }
            else if (account.LevelNumber >= 75 && account.LevelNumber < 100) // gold
            {
                cardColor = Color.Gold;
            }
            else if (account.LevelNumber >= 100) // diamond
            {
                cardColor = Color.Blue;
            }

            var embed = new EmbedBuilder();
            embed.WithThumbnailUrl(target.GetAvatarUrl()); // image of cog to represent settings
            embed.AddField("User", target.Mention, true);
            embed.AddField("XP", account.XP, true);
            embed.AddField("Level", account.LevelNumber, true);
            embed.AddField("Points", account.Points, true);
            embed.AddField("Rank", rank, false);
            // TODO add XP until next rank
            embed.WithColor(cardColor);

            await Context.Channel.SendMessageAsync("", embed: embed.Build());
        }

        [Command("Leaderboard")]
        public async Task Leaderboard(string args1 = "1", [Remainder]string args2 = "1")
        {
            int pageSize = 10;
            bool argsIsAnInteger = int.TryParse(args1, out int intValue);
            bool args2IsAnInteger = int.TryParse(args2, out intValue);

            if (args1 == "global")
            {
                if (!args2IsAnInteger)
                {
                    await ReplyAsync("", embed: await EmbedHandler.CreateBasicEmbed("🌍 Global Leaderboard", $"Global leaderboard page must be an integer", Color.Red));
                    return;
                }

                int pageNumber = Convert.ToInt32(args2);
                if (pageNumber < 1 || pageNumber > Global.Config.MaxLeaderboardPage)
                {
                    await ReplyAsync("", embed: await EmbedHandler.CreateBasicEmbed("🌍 Global Leaderboard", $"Global leaderboard page must between 1 and {Global.Config.MaxLeaderboardPage}", Color.Red));
                    return;
                }

                var userAccounts = Accounts.GetAccounts();
                var gLeaderboardAccounts = userAccounts
                    .OrderByDescending(acc => acc.XP)
                    .Select(u => GetUsernameById(u.ID, Context.Client.GetGuild(u.GuildID)))
                    .ToList();
                
                var pageEndValue = Global.Config.LeaderboardSize + ((pageNumber - 1) * pageSize);
                var pageStartValue = pageEndValue - pageSize;

                var embed = new EmbedBuilder();
                for (int i = pageStartValue; i < gLeaderboardAccounts.Count; i++)
                {
                    embed.AddField($"#{i + 1} - {GetUserXP(gLeaderboardAccounts[i])} XP", $"{gLeaderboardAccounts[i].Mention} [{GetUsernameById(gLeaderboardAccounts[i].Id, gLeaderboardAccounts[i].Guild).Guild.Name}]", false);
                }
                embed.WithTitle($"🌍 **Global Leaderboard**");
                embed.WithColor(Color.DarkBlue);
                embed.WithThumbnailUrl(Context.Client.CurrentUser.GetAvatarUrl());
                embed.WithFooter($"Page {pageNumber} • Users with XP: {gLeaderboardAccounts.Count}");

                await ReplyAsync("", embed: embed.Build());
            }
            else // local
            {
                if (!argsIsAnInteger)
                {
                    await ReplyAsync("", embed: await EmbedHandler.CreateBasicEmbed("🏆 Leaderboard", $"Leaderboard page must be an integer", Color.Red));
                    return;
                }

                int pageNumber = Convert.ToInt32(args1);
                if (pageNumber < 1 || pageNumber > Global.Config.MaxLeaderboardPage)
                {
                    await ReplyAsync("", embed: await EmbedHandler.CreateBasicEmbed("🏆 Leaderboard", $"Leaderboard page must between 1 and {Global.Config.MaxLeaderboardPage}", Color.Red));
                    return;
                }
                var userAccounts = Accounts.GetAccountsInGuild(Context.Guild);
                var leaderboardAccounts = userAccounts
                    .OrderByDescending(acc => acc.XP)
                    .Select(u => GetUsernameById(u.ID, Context.Guild))
                    .ToList();

                var embed = new EmbedBuilder();
                var pageEndValue = Global.Config.LeaderboardSize + ((pageNumber - 1) * pageSize);
                var pageStartValue = pageEndValue - pageSize;
                Console.WriteLine("Page start value: " + pageStartValue);
                Console.WriteLine("Page end value: " + pageEndValue);

                for (int i = pageStartValue; i < leaderboardAccounts.Count; i++)
                {
                    embed.AddField($"#{i + 1} - {GetUserXP(leaderboardAccounts[i])} XP", $"{leaderboardAccounts[i].Mention}", false);
                }
                embed.WithTitle($"🏆 **{Context.Guild.Name} Leaderboard**");
                embed.WithColor(Color.Teal);
                embed.WithThumbnailUrl(Context.Guild.IconUrl);
                embed.WithFooter($"Page {pageNumber} • Users with XP: {leaderboardAccounts.Count}");

                await ReplyAsync("", embed: embed.Build());
            }
        }
        private SocketGuildUser GetUsernameById(ulong userID, SocketGuild guild)
        {
            var target = Context.Client.GetGuild(guild.Id).GetUser(userID);
            return target;
        }
        private uint GetUserXP(SocketGuildUser user)
        {
            var account = Accounts.GetAccount(user);
            return account.XP;
        }
    }
}