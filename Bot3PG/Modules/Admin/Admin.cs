using Bot3PG.Data;
using Bot3PG.Data.Structs;
using Bot3PG.Handlers;
using Discord;
using Discord.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Bot3PG.Modules.Admin
{
    [Color(80, 55, 80)]
    [RequireUserPermission(GuildPermission.Administrator)]
    [RequireBotPermission(GuildPermission.Administrator)]
    public sealed class Admin : CommandBase
    {
        internal override string ModuleName => "Admin 🔒";
        internal override Color ModuleColour => Color.Purple;

        [Command("Announce")]
        [Summary("Send a direct message to all users in a server")]
        public async Task Announce([Remainder] string message)
        {
            var guildUsers = Context.Guild.Users;
            int count = 0;
            foreach (var guildUser in guildUsers.Where(u => !u.IsBot))
            {
                try
                {                    
                    await guildUser.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed($"`{Context.Guild.Name}` - Announcement", message, Color.DarkTeal));                    
                    count++;
                }
                catch {}
            }
            await ReplyAsync(EmbedHandler.CreateSimpleEmbed(ModuleName, $"Message sent to `{count}` users", Color.Green));
        }

        [Command("Embed")]
        [Summary("Create a custom embed. Separate Title, Description, and Image URL with '|' (vertical bar) ")]
        public async Task Embed([Remainder] string details)
        {
            var features = details.Split("|");
            if (features.Length <= 1 || features.Length > 3)
            {
                await ReplyAsync(EmbedHandler.CreateErrorEmbed(ModuleName, "Please separate Title, Description, and Image URL with '|' (vertical bar)"));
                return;
            }

            var embed = new EmbedBuilder();
            embed.WithTitle(features[0]);
            embed.WithDescription(features[1]);
            embed.WithThumbnailUrl(features.Length > 2 ? features[2] : "");
            embed.WithColor(Color.DarkGreen);
            
            await ReplyAsync(embed);
        }

        [Command("Image"), Alias("Img")]
        [Summary("Get bot to send image URL")]
        public async Task SendImage([Remainder] string url)
        {
            try
            {
                if (url is null)
                    throw new ArgumentException("Command argument must contain image URL.");
                
                var embed = new EmbedBuilder();
                embed.WithImageUrl(url);
                await ReplyAsync(embed);
            }
            catch (ArgumentException ex) { await ReplyAsync(EmbedHandler.CreateErrorEmbed(ModuleName, ex.Message)); }
        }

        [Command("Prefix")]
        [Summary("Quickly view/change your server prefix without using the dashboard")]
        public async Task SetPrefix([Remainder] string prefix = "")
        {
            try
            {
                CurrentGuild ??= await Guilds.GetAsync(Context.Guild);

                if (string.IsNullOrEmpty(prefix))
                {
                    await ReplyAsync(EmbedHandler.CreateBasicEmbed(ModuleName, $"**Current Prefix**: `{CurrentGuild.General.CommandPrefix}`", ModuleColour));
                    return;
                }                
                const int maxLength = 16;
                if (prefix.Length > maxLength)
                    throw new ArgumentException($"Prefix must be less than {maxLength + 1} characters long.");

                CurrentGuild.General.CommandPrefix = prefix;
                await Guilds.Save(CurrentGuild);

                await ReplyAsync(EmbedHandler.CreateBasicEmbed(ModuleName, $"Prefix has been set to `{prefix}`", Color.Green));
            }
            catch (ArgumentException ex) { await ReplyAsync(EmbedHandler.CreateErrorEmbed(ModuleName, ex.Message)); }
        }

        [Command("Reset")]
        [Summary("Reset server settings to their default values")]
        public async Task ResetGuild()
        {
            await Guilds.ResetAsync(Context.Guild);
            await ReplyAsync(EmbedHandler.CreateSimpleEmbed(ModuleName, "Succesfully Reset Server Settings 🔃", Color.Green));
        }

        [Command("Rulebox")]
        [Summary("Create rule agreement embed")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task CreateRulebox()
        {
            var embed = new EmbedBuilder();
            embed.WithTitle(CurrentGuild.Admin.Rulebox.Message);

            var rulebox = await ReplyAsync(embed);

            CurrentGuild.Admin.Rulebox.MessageId = rulebox.Id;
            CurrentGuild.Admin.Rulebox.Channel = rulebox.Channel.Id;

            await AddRuleboxReactions(CurrentGuild, rulebox);
            await Guilds.Save(CurrentGuild);
        }

        private async Task AddRuleboxReactions(Guild guild, IUserMessage rulebox)
        {
            var agreeEmote = new Emoji(guild.Admin.Rulebox.AgreeEmote) as IEmote;
            var disagreeEmote = new Emoji(guild.Admin.Rulebox.DisagreeEmote) as IEmote;
            
            await rulebox.AddReactionAsync(agreeEmote);
            await rulebox.AddReactionAsync(disagreeEmote);
            await rulebox.PinAsync();
        }

        [Command("Say")]
        [Summary("Get the bot to say message")]
        public async Task Say([Remainder] string message) => await ReplyAsync(message);
    }
}