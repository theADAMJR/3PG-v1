using Bot3PG.Data;
using Bot3PG.Handlers;
using Discord;
using Discord.Commands;
using System;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Linq;

namespace Bot3PG.Modules.General
{
    [Color(65, 50, 130)]
    public sealed class General : CommandBase
    {
        internal override string ModuleName => "General";
        internal override Color ModuleColour => Color.Teal;

        public Lazy<CommandHelp> CommandHelp => new Lazy<CommandHelp>();
        private CommandHelp Commands => CommandHelp.Value;


        [Command("Help"), Alias("?")]
        [Summary("Show commands link")]
        public async Task Help()
        {
            CurrentGuild ??= await Guilds.GetAsync(Context.Guild);
            var prefix = CurrentGuild.General.CommandPrefix;
            string prefixQuery = prefix != "/" ? $"?prefix={prefix}" : "";
            await ReplyAsync(EmbedHandler.CreateBasicEmbed($"View all commands", $"{Global.Config.WebappLink}/commands{prefixQuery}", Color.DarkPurple));
        }

        [Command("Ping")]
        [Summary("Display bot reponse speed")]
        public async Task Ping()
        {
            var ping = new Ping();
            var reply = ping.Send(Global.DatabaseConfig.Server, 1000);
            var embed = await EmbedHandler.CreateSimpleEmbed("Pong! 🏓", $"**Database:** {reply.RoundtripTime}ms\n **Latency:** {Global.Client.Latency}ms", Color.Magenta);
            await ReplyAsync(embed);
        }

        [Command("Bot")]
        [Summary("Display bot statstics")]
        public async Task Bot()
        {
            string creationDate = $"{Context.Client.CurrentUser.CreatedAt.Day}/{Context.Client.CurrentUser.CreatedAt.Month}/{Context.Client.CurrentUser.CreatedAt.Year}";

            var embed = new EmbedBuilder();
            embed.WithTitle($"{Context.Client.CurrentUser.Username} stats");
            embed.AddField("Creator", Context.Client.CurrentUser.Mention, true);
            embed.AddField("Servers", Context.Client.Guilds.Count, true);
            embed.AddField("Uptime", $"{Global.Uptime.Days}d {Global.Uptime.Hours}h {Global.Uptime.Minutes}m {Global.Uptime.Seconds}s", true);
            embed.AddField("Creation Date", creationDate, true);
            embed.AddField("DM Channels", Context.Client.DMChannels.Count, true);
            embed.AddField("Processor Count", Environment.ProcessorCount, true);
            embed.AddField("Page Size", $"{Environment.SystemPageSize} bytes", true);
            embed.WithThumbnailUrl(Context.Client.CurrentUser.GetAvatarUrl());
            embed.WithFooter($"Bot ID: {Context.Client.CurrentUser.Id}");
            embed.WithColor(Color.DarkMagenta);

            await ReplyAsync(embed);
        }

        [Command("Stats")]
        [Summary("Show server stats")]
        public async Task Stats()
        {
            var embed = new EmbedBuilder();
            embed.WithThumbnailUrl(Context.Guild.IconUrl);
            embed.WithColor(Color.DarkMagenta);
            embed.AddField("Owner", Context.Guild.Owner.Mention, true);
            embed.AddField("Default Channel", Context.Guild.DefaultChannel.Mention, true);
            embed.AddField("Member Count", Context.Guild.MemberCount, true);
            embed.AddField("Creation Date", Context.Guild.CreatedAt.ToString("dd/MM/yy"), true);
            embed.AddField("Role Count", Context.Guild.Roles.Count, true);
            embed.AddField("Channel Count", Context.Guild.Channels.Count, true);
            embed.AddField("Text Channels", Context.Guild.TextChannels.Count, true);
            embed.AddField("Voice Channels", Context.Guild.VoiceChannels.Count, true);
            embed.WithFooter($"Server Name: {Context.Guild.Name} | ServerID: {Context.Guild.Id}");

            await ReplyAsync(embed);
        }

        [Command("Suggest")]
        [Summary("Suggest a new feature"), Remarks("Seperate *Title*, *Subtitle*, and *Description* with | (vertical bar)")]
        public async Task Suggest([Remainder] string details)
        {
            var features = details.Split("|");
            if (features.Length < 3)
            {
                await ReplyAsync(EmbedHandler.CreateBasicEmbed("Suggest", "Please seperate *Title*, *Subtitle*, and *Description* with | (vertical bar).", Color.Red));
                return;
            }
            if (string.IsNullOrWhiteSpace(details.Replace("|", "")))
            {
                await ReplyAsync(EmbedHandler.CreateBasicEmbed("Suggest", "Please add content to the suggestion.", Color.Red));
                return;
            }
            string title = features[0];
            string subtitle = features[1];
            string description = features[2];
            
            var embed = new EmbedBuilder();
            embed.WithTitle(title);
            embed.AddField(subtitle, description);
            embed.WithColor(Color.DarkBlue);
            embed.WithFooter($"By {Context.User.Username}#{Context.User.Discriminator}");
            embed.WithCurrentTimestamp();

            var suggestMessage = await ReplyAsync(embed);

            Emoji[] emotes = { new Emoji(CurrentGuild.General.UpvoteEmote), new Emoji(CurrentGuild.General.DownvoteEmote)};
            emotes = emotes.Where(e => e != null).ToArray();
            await suggestMessage.AddReactionsAsync(emotes);
        }

        [Command("Flip"), Alias("Coin", "Coinflip", "CF")]
        [Summary("Flip a coin, with a psuedo-random result - heads or tails?")]
        public async Task Flip()
        {
            var random = new Random();
            bool heads = random.Next(0, 2) == 1;
            await ReplyAsync(EmbedHandler.CreateBasicEmbed("Coin Flip", $"The coin landed on... `{(heads ? "heads" : "tails")}`", Color.LightOrange));
        }
    }
}