using Bot3PG.Data;
using Bot3PG.Handlers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Linq;
using Bot3PG.Data.Structs;

namespace Bot3PG.Modules.General
{
    [Color(65, 50, 130)]
    public sealed class General : CommandBase
    {
        public Lazy<CommandHelp> CommandHelp => new Lazy<CommandHelp>();
        private CommandHelp Commands => CommandHelp.Value;

        [Command("Help"), Alias("?")]
        [Summary("Show command details or search for commands"), Remarks("**Modules:** Admin, General, Moderation, Music, XP")]
        public async Task Help([Remainder]string module = "")
        {
            var target = Context.User;
            var guild = await Guilds.GetAsync(Context.Guild);
            var prefix = guild.General.CommandPrefix;

            string moduleName = Commands.Modules.FirstOrDefault(m => m.Name.ToLower() == module.ToLower())?.Name;
            if (module != "" && moduleName is null)
            {
                await SearchCommands(target, prefix, module);
                return;
            }
            var embed = new EmbedBuilder();

            string previousModule = Commands.Modules.Select(m => m.Name).Reverse().First();
            ConfigModule configModule = null;
            foreach (var command in Commands.Values.Reverse())
            {
                if (!string.IsNullOrEmpty(module) && command.Module.Name.ToLower() != moduleName.ToLower()) continue;

                if (previousModule != command.Module.Name)
                {
                    embed.WithTitle($"**{Context.Client.CurrentUser.Username} - {previousModule} commands**");

                    if (configModule.Enabled) 
                    {
                        await ReplyToUserAsync(target, embed);
                    }
                    embed = new EmbedBuilder();
                    embed.WithColor(command.Module.Color);
                }
                previousModule = command.Module.Name;
                configModule = guild.GetType().GetProperty(previousModule).GetValue(guild) as ConfigModule;

                embed.AddField($"{prefix}{command.Usage}", $"{command.Summary}", inline: true);
            }

            string prefixQuery = prefix != "/" ? $"?prefix={prefix}" : "";
            embed.WithTitle($"**{Context.Client.CurrentUser.Username} - {previousModule} commands**");
            if (configModule.Enabled)
            {
                await ReplyToUserAsync(target, embed);
            }
            await ReplyToUserAsync(target, await EmbedHandler.CreateBasicEmbed("View all commands", $"{Global.Config.WebappLink}/commands{prefixQuery}", Color.DarkPurple));
        }

        private async Task SearchCommands(SocketUser target, string prefix, string search)
        {
            var embed = new EmbedBuilder();
            embed.WithTitle($"Showing Results for '{search}'");
            var similarCommands = Commands.Where(c => c.Value.Alias.Contains(search) || c.Key.Contains(search)).Take(5).ToArray();
            foreach (var command in similarCommands)
            {
                bool similarToAlias = command.Value.Alias.Contains(search);
                if (similarToAlias || command.Key.Contains(search))
                {                    
                    embed.AddField($"\n{prefix}{command.Key}**", 
                        $"\n**Usage:** {prefix}{command.Value.Usage} " +
                        $"\n**Summary:** {command.Value.Summary}" +
                        $"{(command.Value.Remarks != null ? "\n" + command.Value.Remarks : "")}" +
                        $"\n**Module:** {command.Value.Module.Name}");
                }
            }
            if (similarCommands.Length <= 0)
            {
                embed.AddField("**Results**", "No results found");
            }
            await ReplyToUserAsync(target, embed);
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

        [Command("Embed")]
        [Summary("Create a custom embed")]
        public async Task Embed(string title = "Not set", string url = "", [Remainder] string description = "Not set")
        {
            var embed = new EmbedBuilder();
            embed.WithTitle(title);
            embed.WithDescription(description);
            embed.WithColor(Color.DarkGreen);

            if (url.Contains("http") || url.Contains("data"))
            {
                embed.WithThumbnailUrl(url);
            }
            await ReplyAsync(embed);
        }

        [Command("Suggest")]
        [Summary("Suggest a new feature"), Remarks("Seperate *Title*, *Subtitle*, and *Description* with | (vertical bar)")]
        public async Task Suggest([Remainder] string details)
        {
            var embed = new EmbedBuilder();

            var feature = details.Split("|");
            string title = feature[0];
            string subtitle = feature[1];
            string description = feature[2];
            
            embed.WithTitle(title);
            embed.AddField(subtitle, description);
            embed.WithColor(Color.DarkBlue);
            embed.WithFooter($"By {Context.User.Mention}");
            embed.WithCurrentTimestamp();

            var upvoteEmote = new Emoji("👍");
            var suggestMessage = await ReplyAsync(embed);
            await suggestMessage.AddReactionAsync(upvoteEmote);
        }
    }
}