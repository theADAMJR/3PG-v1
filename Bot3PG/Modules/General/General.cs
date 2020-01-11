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

        [Command("Bot")]
        [Summary("Display bot statstics")]
        public async Task Bot()
        {
            string creationDate = $"{Context.Client.CurrentUser.CreatedAt.Day}/{Context.Client.CurrentUser.CreatedAt.Month}/{Context.Client.CurrentUser.CreatedAt.Year}";

            var embed = new EmbedBuilder()
                .WithTitle($"{Context.Client.CurrentUser.Username} stats")
                .AddField("Creator", Context.Client.GetUser(Global.CreatorID).Mention, true)
                .AddField("Servers", Context.Client.Guilds.Count, true)
                .AddField("Uptime", $"{Global.Uptime.Days}d {Global.Uptime.Hours}h {Global.Uptime.Minutes}m {Global.Uptime.Seconds}s", true)
                .AddField("Creation Date", creationDate, true)
                .AddField("DM Channels", Context.Client.DMChannels.Count, true)
                .AddField("Processor Count", Environment.ProcessorCount, true)
                .AddField("Page Size", $"{Environment.SystemPageSize} bytes", true)
                .WithFooter($"Bot ID: {Context.Client.CurrentUser.Id}")
                .WithThumbnailUrl(Context.Client.CurrentUser.GetAvatarUrl())
                .WithColor(Color.DarkMagenta);

            await ReplyAsync(embed);
        }

        [Command("Flip"), Alias("Coin", "Coinflip", "CF")]
        [Summary("Flip a coin, with a psuedo-random result - heads or tails?")]
        public async Task Flip()
        {
            var random = new Random();
            bool heads = random.Next(0, 2) == 1;
            await ReplyAsync(EmbedHandler.CreateBasicEmbed("Coin Flip", $"The coin landed on... `{(heads ? "heads" : "tails")}`", Color.LightOrange));
        }

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


        [Command("Stats")]
        [Summary("Show server stats")]
        public async Task Stats()
        {
            var embed = new EmbedBuilder()
                .WithThumbnailUrl(Context.Guild.IconUrl)
                .WithColor(Color.DarkMagenta)
                .AddField("Owner", Context.Guild.Owner.Mention, true)
                .AddField("Default Channel", Context.Guild.DefaultChannel.Mention, true)
                .AddField("Member Count", Context.Guild.MemberCount, true)
                .AddField("Creation Date", Context.Guild.CreatedAt.ToString("dd/MM/yy"), true)
                .AddField("Role Count", Context.Guild.Roles.Count, true)
                .AddField("Channel Count", Context.Guild.Channels.Count, true)
                .AddField("Text Channels", Context.Guild.TextChannels.Count, true)
                .AddField("Voice Channels", Context.Guild.VoiceChannels.Count, true)
                .WithFooter($"Server Name: {Context.Guild.Name} | ServerID: {Context.Guild.Id}");

            await ReplyAsync(embed);
        }

        [Command("Suggest")]
        [Summary("Suggest a new feature"), Remarks("Seperate *Title*, *Subtitle*, and *Description* with | (vertical bar)")]
        public async Task Suggest([Remainder] string details)
        {
            try
            {
                var features = details.Split("|");
                if (features.Length < 3)
                    throw new ArgumentException("Suggest", "Please seperate *Title*, *Subtitle*, and *Description* with | (vertical bar).");
                else if (string.IsNullOrWhiteSpace(details.Replace("|", "")))
                    throw new ArgumentException("Suggest", "Please add content to the suggestion.");

                string title = features[0];
                string subtitle = features[1];
                string description = features[2];
                
                var embed = new EmbedBuilder()
                    .WithTitle(title)
                    .AddField(subtitle, description)
                    .WithColor(Color.DarkBlue)
                    .WithFooter($"By {Context.User.Username}#{Context.User.Discriminator}")
                    .WithCurrentTimestamp();

                var suggestMessage = await ReplyAsync(embed);

                Emoji[] emotes = { new Emoji(CurrentGuild.General.UpvoteEmote), new Emoji(CurrentGuild.General.DownvoteEmote)};
                emotes = emotes.Where(e => e != null).ToArray();
                await suggestMessage.AddReactionsAsync(emotes);
            }
            catch (ArgumentException ex) { await ReplyAsync(EmbedHandler.CreateErrorEmbed(ModuleName, ex.Message)); }
        }

        [Command("Support")]
        [Summary("Get a link to the 3PG support Discord server")]
        public async Task SendSupportMessage() 
            => await ReplyAsync(EmbedHandler.CreateSimpleEmbed("Support 💬", $"**3PG Discord Server**: {Global.Config.WebappLink}/support", Color.Purple));
    }
}