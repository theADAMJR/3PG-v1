using Bot3PG.Core.Data;
using Bot3PG.DataStructs;
using Bot3PG.DataStructs.Attributes;
using Bot3PG.Handlers;
using Bot3PG.Modules;
using Bot3PG.Utilities;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Bot3PG.CommandModules
{
    [RequireContext(ContextType.Guild)]
    public sealed class Admin : CommandBase
    {
        [Command("Config"), Alias("Settings")]
        [Summary("View server preferences"), Release(Release.Unstable)]
        [Remarks("**Modules:** Admin, All, General, Moderation, Music, XP")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task Config(string module = "all", string submodule = "")
        {
            try
            {
                var guild = await Guilds.GetAsync(Context.Guild);
                var embed = new EmbedBuilder();

                if (module == "all")
                {
                    await ReplyMainConfig(guild);
                    return;
                }

                var flags = BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance;

                var configModule = typeof(Guild).GetProperty(module, flags);
                var configSubmodule = configModule.PropertyType.GetProperty(submodule, flags);

                var requestedModule = string.IsNullOrEmpty(submodule) ? configModule : configSubmodule;
                var requestedModuleProperties = requestedModule.PropertyType.GetProperties(flags);

                var parentValue = string.IsNullOrEmpty(submodule) ? guild : configModule.GetValue(guild);
                var propertyParentValue = requestedModule.GetValue(parentValue);

                foreach (var property in requestedModuleProperties)
                {
                    dynamic propertyValue = property.GetValue(propertyParentValue);
                    var propertyIsConfigurable = property.GetCustomAttribute(typeof(NotConfigurableAttribute)) is null;
                    if (!propertyIsConfigurable) return;

                    var propertySpecialVersion = property.GetCustomAttribute(typeof(ReleaseAttribute)) as ReleaseAttribute;
                    var propertyIsPremium = property.GetCustomAttribute(typeof(PremiumAttribute)) != null;

                    var descriptionAttribute = property.GetCustomAttribute(typeof(DescriptionAttribute)) as DescriptionAttribute;
                    string description = descriptionAttribute?.Description ?? "No description set.";

                    if (propertyValue is ConfigModule.SubModule subModule)
                    {
                        string name = $"{GetConfigValue(subModule)} ";
                        name += propertyIsPremium && !guild.IsPremium ? $"**~~{property.Name}~~**" : $"**{property.Name}**";

                        embed.AddField(name, description);
                    }
                    else if (propertyIsConfigurable)
                    {
                        string name = propertyIsPremium && !guild.IsPremium ? $"~~{property.Name}~~: {GetConfigValue(propertyValue)}" : $"{property.Name}: {GetConfigValue(propertyValue)}";
                        embed.AddField(name, description);
                    }
                }
                await ReplyAsync(embed);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private async Task ReplyMainConfig(Guild guild)
        {
            var embed = new EmbedBuilder();
            var moduleProperties = guild.GetType().GetProperties();

            foreach (var moduleProperty in moduleProperties)
            {
                var moduleValue = moduleProperty.GetValue(guild);
                if (moduleValue is ConfigModule configModule)
                {
                    var name = moduleProperty.Name;
                    var command = $"{guild.General.CommandPrefix}config {name.ToLower()}";
                    var attributes = configModule.GetType().GetCustomAttributes(true);

                    string description = "No description set";
                    bool moduleIsPremium = false;
                    foreach (var attribute in attributes)
                    {
                        moduleIsPremium = (attribute is PremiumAttribute) ? true : moduleIsPremium;
                        name = moduleIsPremium && !guild.IsPremium ? $"~~{moduleProperty.Name}~~" : $"**{moduleProperty.Name}**";
                        description = (attribute as DescriptionAttribute)?.Description ?? description;
                    }
                    embed.AddField($"{GetConfigValue(configModule)} {name}", $"`{command}` - {description}");
                }
            }
            await ReplyAsync(embed);
        }

        [Command("Config"), Alias("Settings")]
        [Summary("Configure server preferences")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task Config(string module, string property, string value)
        {
            var guild = await Guilds.GetAsync(Context.Guild);               
            value = ValidateNewValue(value);

            var flags = BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance;
            var configModule = typeof(Guild).GetProperty(module, flags);
            var configModuleProperty = configModule.PropertyType.GetProperty(property, flags);

            var configModuleValue = configModule.GetValue(guild);
            if (Convert.ChangeType(value, configModuleProperty.PropertyType) is null)
            {
                throw new InvalidOperationException($"Value must be of type {configModuleProperty.PropertyType}");
            }
            configModuleProperty.SetValue(configModuleValue, value);
            await Guilds.Save(guild);
        }

        [Command("Config"), Alias("Settings")]
        [Summary("Configure server preferences")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task Config(string module, string submodule, string property, string value)
        {
            var guild = await Guilds.GetAsync(Context.Guild);
            value = ValidateNewValue(value);

            var flags = BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance;
            var configModule = typeof(Guild).GetProperty(module, flags);
            var configSubmodule = configModule.PropertyType.GetProperty(submodule, flags);
            var configSubmoduleProperty = configSubmodule.PropertyType.GetProperty(property, flags);

            var configModuleValue = configModule.GetValue(guild);
            var configSubmoduleValue = configModule.GetValue(configModuleValue);

            configSubmoduleProperty.SetValue(configSubmoduleValue, value);
            await Guilds.Save(guild);
        }

        private string ValidateNewValue(string newValue)
        {
            if (newValue.ToString().Contains("#"))
            {
                newValue = newValue.Replace(" ", "");
                newValue = newValue.Replace(">", "");
                newValue = newValue.Replace("<", "");
                newValue = newValue.Replace("#", "");
                var newChannel = Context.Guild.TextChannels.FirstOrDefault(c => c.Id == Convert.ToUInt64(newValue));
                return Context.Guild.GetChannel(newChannel.Id).ToString() ?? newValue;
            }
            return newValue;
        }

        private async Task ResetGuildAsync(string arguments, string module, string submodule, string argument)
        {
            await Guilds.ResetAsync(Context.Guild);
        }

        private static string ValidatePropertyName(string propertyName)
        {
            propertyName = propertyName.ToLower();
            propertyName = propertyName.Replace($"{'"'}", "");
            return propertyName;
        }

        private object GetConfigValue(dynamic value)
        {
            if (value is null)
            {
                return "`UNDEFINED`";
            }

            else if (value is string stringValue)
            {
                return string.IsNullOrEmpty(stringValue) ? "`UNDEFINED`" : "`" + value + "`";
            }
            else if (value is int intValue)
            {
                if (intValue == 0)
                {
                    return "`UNASSIGNED`";
                }
                else if (intValue == -1)
                {
                    return "`DISABLED`";
                }
                return "`" + intValue + "`";
            }
            else if (value is bool boolValue)
            {
                return boolValue ? new Emoji("✅") as IEmote : new Emoji("❌") as IEmote;
            }
            else if (value is ConfigModule configModule)
            {
                bool isPremium = configModule.GetType().GetCustomAttribute(typeof(PremiumAttribute)) != null;
                if (configModule.Enabled)
                {
                    return new Emoji("✅");
                }
                else if (isPremium)
                {
                    return new Emoji("❌");
                }
                return new Emoji("🤖");
            }
            else if (value is IList list)
            {
                var itemString = "";
                for (var i = 0; i < list.Count; i++)
                {
                    itemString += $"\n*[{i + 1}]* {GetConfigValue(list[i])}";
                }
                return itemString;
            }
            return $"`{value}`";
        }

        /*private string GetConfigValue(string value) =>  (value is null) ? "`UNASSIGNED`" : "`" + value + "`";
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
        private IEmote GetConfigValue(ConfigModule configModule, bool isPremium)
        {
            if (configModule.Enabled && !isPremium)
            {
                return new Emoji("✅");
            }
            else if (!isPremium)
            {
                return new Emoji("❌");
            }
            return new Emoji("🤖");
        }*/
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
            guild.Admin.Rulebox.Message = rulebox;

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

        [Command("Disable")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        [Summary("Temporarily disable the bot for a specified amount of time, if previously enabled"), Release(Release.Alpha)]
        public async Task Disable(string duration)
        {
            var guild = await Guilds.GetAsync(Context.Guild);
            if (guild.IsDisabled)
            {
                await ReplyAsync(await EmbedHandler.CreateBasicEmbed("Disable", $"{Context.Client.CurrentUser.Username} already disabled.", Color.DarkRed));
                return;
            }
            var time = CommandUtilities.ParseDuration(duration);
            var enableDateTime = DateTime.Now + time;

            await ReplyAsync(await EmbedHandler.CreateBasicEmbed("Disable", $"{Context.Client.CurrentUser.Username} disabled until {enableDateTime.ToTimestamp()}.", Color.DarkGrey));
            await Guilds.Save(guild);
        }

        [Command("Enable")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        [Summary("Enable the bot, if previously disabled"), Release(Release.Alpha)]
        public async Task Enable()
        {
            var guild = await Guilds.GetAsync(Context.Guild);
            if (!guild.IsDisabled)
            {
                await ReplyAsync(await EmbedHandler.CreateBasicEmbed("Enable", $"{Context.Client.CurrentUser.Username} already enabled.", Color.DarkRed));
                return;
            }
            guild.IsDisabled = false;

            await ReplyAsync(await EmbedHandler.CreateBasicEmbed("Enable", $"{Context.Client.CurrentUser.Username} enabled.", Color.DarkGrey));
            await Guilds.Save(guild);
        }
    }
}