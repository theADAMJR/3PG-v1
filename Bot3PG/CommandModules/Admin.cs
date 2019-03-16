using Bot3PG.Core.Users;
using Bot3PG.Handlers;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Bot3PG.CommandModules
{
    public class Admin : ModuleBase<SocketCommandContext>
    {
        /*[Command("Config")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task BotConfig(string arg = "")
        {
            var embed = new EmbedBuilder();
            embed.WithThumbnailUrl("https://cdn.pixabay.com/photo/2013/07/12/13/50/cog-147414_960_720.png"); // image of cog to represent settings
            embed.WithTitle($"**{Context.Client.CurrentUser.Username} config**");
            embed.AddField("Welcome Messages", Config.bot.botWelcome, true);
            embed.AddField("Command Prefix", Config.bot.cmdPrefix, true);
            embed.AddField("Auto Moderation", Config.bot.autoModeration, true);
            embed.AddField("Mod Commands", Config.bot.modCommands, true);
            embed.AddField("Music Enabled", Config.bot.musicBot, true);
            embed.AddField("XP Enabled", Config.bot.xpBot, true);
            embed.AddField("XP Per Message", Config.bot.xpPerMessage, true);
            embed.AddField("Leaderboard Size", Config.bot.leaderboardSize, true);
            embed.AddField("Warnings for Kick", Config.bot.warningNumberToKick, true);
            embed.AddField("Warnings for Ban", Config.bot.warningNumberToBan, true);
            embed.AddField("__Other__", " ", false);
            embed.AddField("Agree Role", Config.bot.agreeRole, true);

            await Context.Channel.SendMessageAsync("", embed: embed.Build());
        }*/

        // Get the bot to say 'msg'
        [Command("Say")]
        [RequireUserPermission(GuildPermission.Administrator)]
        [RequireBotPermission(GuildPermission.Administrator)]
        public async Task Say([Remainder]string msg = "")
        {
            await ReplyAsync(msg);
        }

        [Command("Image")]
        [RequireUserPermission(GuildPermission.Administrator)]
        [RequireBotPermission(GuildPermission.Administrator)]
        public async Task Image([Remainder]string imgURL = "")
        {
            if (imgURL == null)
            {
                await ReplyAsync("Command argument must contain image URL.");
            }
            else
            {
                var embed = new EmbedBuilder();
                embed.WithImageUrl(imgURL);
                await ReplyAsync("", embed: embed.Build());
            }
        }

        [Command("Rulebox")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task NewRulebox()
        {
            RestUserMessage msg = await Context.Channel.SendMessageAsync("Do you agree to the rules?");
            Global.RuleboxMessageID = msg.Id;

            var agreeEmote = new Emoji("✅") as IEmote;
            var disagreeEmote = new Emoji("❌") as IEmote;
            await msg.AddReactionAsync(agreeEmote);
            await msg.AddReactionAsync(disagreeEmote);
        }

        [Command("Votebox")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task Vote(string title, [Remainder]string allOptions)
        {
            string[] options = allOptions.Split(",");

            if (options.Length < 2 || options.Length > 6)
            {
                Console.WriteLine("error");
                await EmbedHandler.CreateBasicEmbed("Error", "Option length must be 2 - 6", Color.Red);
            }

            var embed = new EmbedBuilder();
            embed.WithTitle($"🗳️ **VOTE** {title}");
            embed.WithColor(Color.DarkTeal);

            var voteEmotes = new IEmote[] { new Emoji("🇦"), new Emoji("🇧"), new Emoji("🇨"), new Emoji("🇩"), new Emoji("🇪"), new Emoji("🇫") };

            int i = 0;
            foreach (string option in options)
            {
                embed.AddField($"**{voteEmotes[i]}** {option}", "__0% votes__", inline: true);
                i++;
            }
            var voteEmbed = await ReplyAsync("", embed: embed.Build());
            Global.VoteboxMessageID = voteEmbed.Id;

            i = 0;
            foreach (string option in options)
            {
                await voteEmbed.AddReactionAsync(voteEmotes[i]);
                i++;
            }
        }

        [Command("AddXP")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task AddXP(SocketGuildUser user, uint amount)
        {
            var account = Accounts.GetAccount(user);
            if (amount > 0)
                account.XP += amount;
            else
                account.XP -= amount;
        }
    }
}