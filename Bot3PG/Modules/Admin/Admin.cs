using Bot3PG.Core.Data;
using Bot3PG.Modules;
using Bot3PG.Modules.General;
using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace Bot3PG.CommandModules
{
    [Color(80, 55, 80)]
    [RequireContext(ContextType.Guild)]
    public sealed class Admin : CommandBase
    {
        [Command("Say")]
        [Summary("Get the bot to say message")]
        [RequireUserPermission(GuildPermission.Administrator, ErrorMessage = ": You must have permissions: Administrator or Manage Guild to use this command")]
        [RequireBotPermission(GuildPermission.Administrator)]
        public async Task Say([Remainder]string message)
        {
            await ReplyAsync(message);
        }

        [Command("Image"), Alias("Img")]
        [Summary("Get bot to send image URL")]
        [RequireUserPermission(GuildPermission.Administrator)]
        [RequireBotPermission(GuildPermission.Administrator)]
        public async Task Image([Remainder]string url)
        {
            if (url is null)
            {
                await ReplyAsync("Command argument must contain image URL.");
                return;
            }
            else
            {
                var embed = new EmbedBuilder();
                embed.WithImageUrl(url);
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
            await AddRuleboxReactions(rulebox);

            var guild = await Guilds.GetAsync(Context.Guild);
            guild.Admin.Rulebox.Id = rulebox.Id;
            guild.Admin.Rulebox.ChannelId = rulebox.Channel.Id;
            await Guilds.Save(guild);
        }

        private async Task AddRuleboxReactions(IUserMessage rulebox)
        {
            var agreeEmote = new Emoji("✅") as IEmote;
            var disagreeEmote = new Emoji("❌") as IEmote;
            await rulebox.AddReactionAsync(agreeEmote);
            await rulebox.AddReactionAsync(disagreeEmote);
            await rulebox.PinAsync();
        }
    }
}