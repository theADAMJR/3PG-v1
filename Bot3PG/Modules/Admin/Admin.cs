using Bot3PG.Data;
using Bot3PG.Data.Structs;
using Bot3PG.Handlers;
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
        public async Task Say([Remainder]string message) => await ReplyAsync(message);

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
            var guild = await Guilds.GetAsync(Context.Guild);

            var embed = new EmbedBuilder();
            embed.WithTitle(guild.Admin.Rulebox.Message);

            var rulebox = await ReplyAsync(embed);

            guild.Admin.Rulebox.MessageId = rulebox.Id;
            guild.Admin.Rulebox.Channel = rulebox.Channel.Id;

            await AddRuleboxReactions(guild, rulebox);
            await Guilds.Save(guild);
        }

        private async Task AddRuleboxReactions(Guild guild, IUserMessage rulebox)
        {
            var agreeEmote = new Emoji(guild.Admin.Rulebox.AgreeEmote) as IEmote;
            var disagreeEmote = new Emoji(guild.Admin.Rulebox.DisagreeEmote) as IEmote;
            
            await rulebox.AddReactionAsync(agreeEmote);
            await rulebox.AddReactionAsync(disagreeEmote);
            await rulebox.PinAsync();
        }

        [Command("Reset")]
        [Summary("Reset server settings to their default values")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task Reset()
        {
            await Guilds.ResetAsync(Context.Guild);
            await ReplyAsync(EmbedHandler.CreateSimpleEmbed("Reset", "Succesfully reset server config", Color.Green));
        }
    }
}