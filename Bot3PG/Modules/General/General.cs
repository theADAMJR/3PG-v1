using System.Diagnostics;
using Bot3PG.Core.Data;
using Bot3PG.Handlers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Victoria;

namespace Bot3PG.Modules.General
{
    [Color(0, 0, 0)]
    public sealed class General : CommandBase
    {
        public CommandHelp Commands => new CommandHelp(new Dictionary<string, Command>(), Global.CommandService);

        [Command("Help"), Alias("?")]
        [Summary("Show command details or search for commands")]
        [Remarks("**Modules:** Admin, All, General, Moderation, Music, XP")]
        public async Task Help([Remainder]string args = "all")
        {
            var target = Context.User;
            var guild = await Guilds.GetAsync(Context.Guild);
            var prefix = guild.Config.CommandPrefix;

            try
            {
                string moduleName = "";
                foreach (var module in Commands.Modules)
                {
                    if (args.ToLower() != module.Name.ToLower()) continue;
                    moduleName = module.Name;
                }

                if (string.IsNullOrEmpty(moduleName))
                {
                    await SearchCommands(target, prefix, args);
                    return;
                };

                var embed = new EmbedBuilder();
                embed.WithTitle($"**{Context.Client.CurrentUser.Username} - {moduleName} commands**");

                bool displayAllCommands = args.ToLower() == "all";
                string previousModule = "";
                foreach (var command in Commands.Values)
                {
                    if (previousModule != command.Module.Name && !displayAllCommands)
                    {
                        embed.WithTitle($"**{Context.Client.CurrentUser.Username} - {previousModule} commands**");
                        await ReplyToUserAsync(target, embed);

                        embed = new EmbedBuilder();
                        embed.WithColor(command.Module.Color);
                        previousModule = command.Module.Name;
                    }
                    else if (command.Module.Name.ToLower() != moduleName.ToLower() && !displayAllCommands) continue;

                    embed.AddField($"{prefix}{command.Usage}", $"{command.Summary}\n{command.Remarks}", true);
                }
                embed.WithTitle($"**{Context.Client.CurrentUser.Username} - {previousModule} commands**");
                await ReplyToUserAsync(target, embed);
            }
            catch
            {
                await SearchCommands(target, prefix, args);
            }
        }

        private async Task SearchCommands(SocketUser target, string prefix, string search)
        {
            var embed = new EmbedBuilder();
            int results = 0;
            foreach (var command in Commands)
            {
                bool similarToAlias = false;
                foreach (var alias in command.Value.Alias)
                {
                    similarToAlias = alias.Contains(search);
                }
                if (similarToAlias || command.Key.Contains(search))
                {
                    embed.WithTitle($"Search for '{search}`");
                    embed.AddField($"\n**{prefix}{command.Key}**", 
                        $"\n**Usage:** {prefix}{command.Value.Usage} " +
                        $"\n**Summary:** {command.Value.Summary}" +
                        $"\n**Info:** {command.Value.Remarks}" +
                        $"\n**Module:** {command.Value.Module.Name}");
                    results++;
                }
            }
            if (results >= 0)
            {
                embed.AddField($"Search for '{search}'", $"No results found for **{search}**");
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
        [Summary("Display bot details")]
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
        public async Task Embed(string title = "Not set", string imgUrl = "", [Remainder] string description = "Not set")
        {
            var embed = new EmbedBuilder();
            embed.WithTitle(title);
            embed.WithDescription(description);
            embed.WithColor(Color.DarkGreen);

            if (imgUrl.Contains("http") || imgUrl.Contains("data"))
            {
                embed.WithThumbnailUrl(imgUrl);
            }
            await ReplyAsync(embed);
        }

        [Command("Suggest")]
        [Summary("Suggest a new feature")]
        [Remarks("Seperate *Title*, *Module*, and *Description* with a comma")]
        public async Task Suggest([Remainder] string featureDetails = "No description")
        {
            var embed = new EmbedBuilder();

            var details = featureDetails.Split(",");
            if (details.Length < 3)
            {
                await ReplyAsync(EmbedHandler.CreateErrorEmbed("Suggest Feature", "Title, Module, and Description must be separated with ','"));
                return;
            }
            string featureTitle = details[0];
            string featureModule = details[1];
            string featureDescription = details[2];

            for (int i = 3; i < details.Length; i++)
            {
                featureDescription += details[i];

            }
            embed.WithTitle(featureTitle);
            embed.WithDescription(featureDescription);
            embed.WithFooter($"Module: {featureModule}");
            embed.WithColor(Color.DarkBlue);
            embed.WithCurrentTimestamp();

            IEmote upvoteEmote = new Emoji("👍");
            var suggestMessage = await ReplyAsync(embed);
            await suggestMessage.AddReactionAsync(upvoteEmote);
        }
    }
}