using Bot3PG.Core.Data;
using Bot3PG.DataStructs;
using Bot3PG.Handlers;
using Bot3PG.Modules;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Bot3PG.CommandModules
{
    [RequireContext(ContextType.Guild)]
    public sealed class Admin : CommandBase
    {
        [Command("Config"), Alias("Settings")]
        [Summary("View server preferences")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task Config(string module = "")
        {
            var guild = await Guilds.GetAsync(Context.Guild);

            switch (module.ToLower())
            {
                case "general":
                    await GeneralConfig(guild.Config);
                    break;
                case "xp":
                    await XPConfig(guild.Config);
                    break;
                case "music":
                    await MusicConfig(guild.Config);
                    break;
                case "moderation":
                    await ModerationConfig(guild.Config);
                    break;
                case "admin":
                    await AdminConfig(guild.Config);
                    break;
                default:
                    await MainConfig(guild.Config);
                    break;
            }
        }

        [Command("Config"), Alias("Settings")]
        [Summary("Configure server preferences")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task Config(string action, string columnName, string newValue = "")
        {
            newValue = ValidateNewValue(newValue);
            if (string.IsNullOrEmpty(columnName) || string.IsNullOrEmpty(newValue.ToString()))
            {
                string errorMessage = string.IsNullOrEmpty(columnName) ? "Config value must be specified." : "New value must be specified.";
                await ReplyAsync(EmbedHandler.CreateErrorEmbed("Guild Config", errorMessage));
                return;
            }

            switch (action.ToLower())
            {
                case "set":
                    await UpdateGuildConfigAsync(columnName, newValue);
                    return;
                //case "reset":
                //    return await ResetGuildConfig(columnName, newValue);
                default:
                    await ReplyAsync(EmbedHandler.CreateErrorEmbed("Guild Config Modify", $"Invalid argument: '{columnName}'"));
                    return;
            }
        }

        private string ValidateNewValue(string newValue)
        {
            if (newValue.ToString().Contains("#"))
            {
                Console.WriteLine(newValue);
                newValue = newValue.Replace(">", "");
                newValue = newValue.Replace("<", "");
                newValue = newValue.Replace("#", "");
                var newChannel = Context.Guild.TextChannels.FirstOrDefault(c => c.Id == Convert.ToUInt64(newValue));
                return Context.Guild.GetChannel(newChannel.Id).ToString() ?? newValue;
            }
            return newValue;
        }

        private async Task MainConfig(Guild.Options config)
        {
            var guild = await Guilds.GetAsync(Context.Guild);
            var command = $"{config.CommandPrefix}config ";

            var embed = new EmbedBuilder();
            string test = "";
            var guildProperties = typeof(Guild.Options).GetProperties();
            foreach (var property in guildProperties)
            {
                // if not current module continue

                var attributes = property.GetCustomAttributes(true);
                foreach (var attribute in attributes)
                {
                    string name = attribute is PremiumAttribute && !guild.IsPremium ? $"~~{property.Name}~~" : $"`{property.Name}`";
                    string description = (attribute as DescriptionAttribute)?.Description ?? "No description set.";
                    test += $"{name} {description}\n";
                }
            }

            //embed.WithTitle($"**⚙️ __{Context.Client.CurrentUser.Username} Config__**");
            //embed.AddField($"{GetConfigValue(config.XPEnabled)} **General** `{command} general`", $"General user commands", inline: false);
            //embed.AddField($"{GetConfigValue(config.XPEnabled)} **XP** `{command} xp`", $"XP module and functionality ", inline: false);
            //embed.AddField($"{GetConfigValue(config.MusicEnabled)} **Music** `{command} music`", "Music module and functionality", inline: false);
            //embed.AddField($"{GetConfigValue(config.MusicEnabled)} **Admin** `{command} admin`", "Admin only commands", inline: false);
            //embed.AddField($"{GetConfigValue(config.ModerationEnabled)} **Moderation**` {command} moderation`", "Moderation commands for enforcing rules", inline: false);
            await ReplyAsync(test);
        }

        [Obsolete]
        private async Task UpdateGuildConfigAsync(string columnName, object newValue)
        {
            var embed = new EmbedBuilder();

            var validColumnName = ValidateColumnName(columnName);
            if (string.IsNullOrEmpty(columnName) || string.IsNullOrEmpty(newValue.ToString()))
            {
                await ReplyAsync(EmbedHandler.CreateErrorEmbed("Update Guild Config", $"Please enter a valid config value"));
                return;
            }

            if (Guilds.ReadOnlyColumns.Any(str => validColumnName.Contains(str)))
            {
                await ReplyAsync(EmbedHandler.CreateErrorEmbed("Update Guild Config", $"Cannot change {validColumnName} as it is read-only!"));
                return;
            }
            else if (Guilds.GetConfigColumn(validColumnName))
            {
                // TODO - set value validation checks
                Guilds.UpdateGuildConfig(Context.Guild, validColumnName, newValue);
                await ReplyAsync(EmbedHandler.CreateBasicEmbed("Config", $"Set __{validColumnName}__ to {newValue}", Color.Green));
                return;
            }
            else
            {
                var similarColumns = Guilds.SearchGuildConfigColumns(validColumnName);
                if (similarColumns.Count < 1)
                {
                    await EmbedHandler.CreateErrorEmbed("Update Guild Config", $"No results found similar to {columnName}");
                    
                }

                string columnList = "";
                for (int i = 0; i < similarColumns.Count; i++)
                {
                    columnList += ($"\n**{i + 1})** {similarColumns[i]}");
                }
                columnList = columnList ?? "No results";
                embed.AddField($"Config Value: '{columnName}' was not found. Similar results:", columnList);
                await ReplyAsync(embed);
            }
        }

        //private async Task ResetGuildConfig(string columnName, string newValue, string module = "")
        //{
        //    return await;
        //}

        private static string ValidateColumnName(string columnName)
        {
            columnName = columnName.ToLower();
            if (columnName.Contains(' '))
            {
                columnName = columnName.Replace(' ', '_');
            }
            if (columnName.Contains('"'))
            {
                columnName = columnName.Replace("'", "");
            }
            return columnName;
        }

        [Obsolete]
        private async Task GeneralConfig(Guild.Options config)
        {
            var embed = new EmbedBuilder();
            embed.WithThumbnailUrl(Context.Client.CurrentUser.GetAvatarUrl());
            embed.WithTitle($"**⚙️Config - 👥 General**");
            embed.AddField($"Command Prefix", GetConfigValue(config.CommandPrefix), inline: true);
            embed.WithColor(ModuleColour(true));
            await ReplyAsync(embed);
        }

        [Obsolete]
        private async Task XPConfig(Guild.Options config)
        {
            var embed = new EmbedBuilder();
            embed.WithThumbnailUrl(Context.Client.CurrentUser.GetAvatarUrl());
            embed.WithTitle($"**⚙️Config - ⭐ XP module**");
            embed.AddField($"XP Enabled", GetConfigValue(config.XPEnabled), inline: false);
            embed.AddField($"XP Per Message", GetConfigValue(config.XPPerMessage), inline: true);
            embed.AddField($"XP Message Length Threshold", GetConfigValue(config.XPMessageLengthThreshold), inline: true);
            embed.AddField($"XP Cooldown", GetConfigValue(config.XPCooldown), inline: true);
            embed.AddField($"Extended XP Cooldown", GetConfigValue(config.ExtendedXPCooldown), inline: true);
            embed.AddField($"Max Leaderboard Page", GetConfigValue(config.MaxLeaderboardPage), inline: true);
            embed.WithColor(Color.DarkBlue);
            await ReplyAsync(embed);
        }
        [Obsolete]
        private async Task MusicConfig(Guild.Options options)
        {
            var embed = new EmbedBuilder();
            embed.WithThumbnailUrl(Context.Client.CurrentUser.GetAvatarUrl());
            embed.WithTitle($"**⚙️Config - 🎶 [ALPHA] Music module**");
            embed.AddField($"Music Enabled", GetConfigValue(options.MusicEnabled), inline: false);
            embed.AddField($"Default Volume", GetConfigValue(options.DefaultVolume), inline: true);
            embed.WithColor(Color.Blue);
            await ReplyAsync(embed);
        }
        [Obsolete]
        private async Task ModerationConfig(Guild.Options options)
        {
            var embed = new EmbedBuilder();
            embed.WithThumbnailUrl(Context.Client.CurrentUser.GetAvatarUrl());
            embed.WithTitle($"**⚙️Config - 🛡️ Moderation module**");
            embed.AddField($"Auto Moderation Enabled", GetConfigValue(options.AutoModerationEnabled), inline: true);
            embed.AddField($"Staff Logs Enabled", GetConfigValue(options.StaffLogsEnabled), inline: true);
            embed.AddField($"Warnings For Ban", GetConfigValue(options.WarningsForBan), inline: true);
            embed.AddField($"Warnings For Kick", GetConfigValue(options.WarningsForKick), inline: true);
            embed.AddField($"Staff Logs Enabled", GetConfigValue(options.StaffLogsEnabled), inline: false);
            embed.AddField($"Staff Logs Channel", GetConfigValue(options.StaffLogsChannel), inline: true);
            embed.AddField($"Rulebox Enabled", GetConfigValue(options.RuleboxEnabled), inline: true);
            embed.WithColor(Color.Orange);
            await ReplyAsync(embed);
        }
        [Obsolete]
        private async Task AdminConfig(Guild.Options options)
        {
            var embed = new EmbedBuilder();
            embed.WithThumbnailUrl(Context.Client.CurrentUser.GetAvatarUrl());
            embed.WithTitle($"**⚙️Config - 👑 Admin module**");
            embed.AddField($"Announce Enabled", GetConfigValue(options.AnnounceEnabled), inline: true);
            embed.AddField($"Announce Channel", GetConfigValue(options.AnnounceChannel), inline: true);
            embed.WithColor(Color.Purple);
             await ReplyAsync(embed);
        }
        private string GetConfigValue(string value) => (value is null) ? "`UNASSIGNED`" : "`" + value + "`";
        private string GetConfigValue(SocketTextChannel channel) => (channel is null) ? "`UNASSIGNED`" : channel.Mention;
        private string GetConfigValue(int value)
        {
            if (value == 0)
            {
                return "`UNASSIGNED`";
            }
            else if (value == -1)
            {
                return "`DISABLED`";
            }
            return "`" + value + "`";
        }
        private IEmote GetConfigValue(bool value) => value ? new Emoji("✅") as IEmote: new Emoji("❌") as IEmote;
        private Color ModuleColour(bool moduleEnabled) => moduleEnabled ? Color.Green : Color.Red;
        private string ModuleText(bool moduleEnabled, string text) => "~~" + text + "~~";

        // Get the bot to say 'msg'
        [Command("Say")]
        [Summary("Get the bot to say message")]
        [RequireUserPermission(GuildPermission.Administrator, ErrorMessage = ": You must have permissions: Administrator or Manage Guild to use this command")]
        [RequireBotPermission(GuildPermission.Administrator)]
        public async Task Say([Remainder]string msg)
        {
            await ReplyAsync(msg);
        }

        [Command("Image"), Alias("Img")]
        [Summary("Get bot to send image URL")]
        [RequireUserPermission(GuildPermission.Administrator)]
        [RequireBotPermission(GuildPermission.Administrator)]
        public async Task Image([Remainder]string imgURL)
        {
            if (imgURL is null)
            {
                await ReplyAsync("Command argument must contain image URL.");
                return;
            }
            else
            {
                var embed = new EmbedBuilder();
                embed.WithImageUrl(imgURL);
                await ReplyAsync(embed);
            }
        }

        [Command("Rulebox")]
        [Summary("Create rule agreement embed")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task Rulebox()
        {
            var embed = new EmbedBuilder();
            embed.WithTitle("Do you agree to the rules?");
            var rulebox = await ReplyAsync(embed);
            var guild = await Guilds.GetAsync(Context.Guild);
            guild.Config.RuleboxMessage = rulebox;

            await AddRuleboxReactions(rulebox);
        }

        private async Task AddRuleboxReactions(IUserMessage rulebox)
        {
            var agreeEmote = new Emoji("✅") as IEmote;
            var disagreeEmote = new Emoji("❌") as IEmote;
            await rulebox.AddReactionAsync(agreeEmote);
            await rulebox.AddReactionAsync(disagreeEmote);
            await rulebox.PinAsync();
        }

        /*[Command("Votebox")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task Votebox(string title, int availableChoices, [Remainder]string allOptions)
        {
            var guild = Guilds.Get(Context.Guild);
            string[] options = allOptions.Split(",");

            if (options.Length < 2 || options.Length > 6)
            {
                await EmbedHandler.CreateBasicEmbed("Error", "Option length must be 2 - 6", Color.Red);
            }

            var embed = new EmbedBuilder();
            embed.WithTitle($"🗳️ **VOTE** {title}");
            embed.WithColor(Color.DarkTeal);

            var voteEmotes = new IEmote[] { new Emoji("🇦"), new Emoji("🇧"), new Emoji("🇨"), new Emoji("🇩"), new Emoji("🇪"), new Emoji("🇫") };

            for (int i = 0; i < options.Length; i++)
            {
                embed.AddField($"**{voteEmotes[i]}** {options[i]}", "__0% votes__"), inline: true);
            }

            var votebox = embed);
            guild.Config.VoteboxMessageID = votebox.Id;

            for (int i = 0; i < options.Length; i++)
            {
                await votebox.AddReactionAsync(voteEmotes[i]);
            }

            var optionsReactions = new List<int>();
            var uniqueVoteEmotes = votebox.Reactions.Values.ToArray();
            Console.WriteLine(uniqueVoteEmotes.Length);
            for (int i = 0; i < uniqueVoteEmotes.Length; i++)
            {
                Console.WriteLine("test");
                if (uniqueVoteEmotes[i].IsMe) Console.WriteLine("is me!");
                optionsReactions.Add(uniqueVoteEmotes[i].ReactionCount);
                await ReplyAsync("% Votes:" + (uniqueVoteEmotes[i].ReactionCount / options.Length) * 100);
            }
        }*/

        /*[Command("VoteboxUpdate")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task VoteboxUpdate(string title, int availableChoices, [Remainder]string allOptions)
        {
            var guild = Guilds.Get(Context.Guild);
            var optionsReactions = new List<int>();
            var votebox = guild.Config.VoteboxMessageID;
            var voteboxMsg = Context.Channel.GetCachedMessage(votebox);
            //var uniqueVoteEmotes = voteboxMsg.re.Values.ToArray();
            Console.WriteLine(uniqueVoteEmotes.Length);
            for (int i = 0; i < uniqueVoteEmotes.Length; i++)
            {
                Console.WriteLine("test");
                if (uniqueVoteEmotes[i].IsMe) Console.WriteLine("is me!");
                optionsReactions.Add(uniqueVoteEmotes[i].ReactionCount);
                await ReplyAsync("% Votes:" + (uniqueVoteEmotes[i].ReactionCount / options.Length) * 100);
            }
        }*/
    }
}