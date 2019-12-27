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
    [RequireUserPermission(GuildPermission.Administrator)]
    [RequireBotPermission(GuildPermission.Administrator)]
    public sealed class Admin : CommandBase
    {
        [Command("Say")]
        [Summary("Get the bot to say message")]
        public async Task Say([Remainder]string message) => await ReplyAsync(message);

        [Command("Image"), Alias("Img")]
        [Summary("Get bot to send image URL")]
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

        [Command("Announce")]
        [Summary("Send a direct message to all users in a server")]
        public async Task Announce([Remainder] string message)
        {
            var socketGuildUsers = Context.Guild.Users;
            int count = 0;
            foreach (var socketGuildUser in socketGuildUsers)
            {
                try
                {
                    if (socketGuildUser.IsBot) continue;
                    
                    await socketGuildUser.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed($"`{Context.Guild.Name}` - Announcement", message, Color.DarkTeal));                    
                    count++;
                }
                catch {}
            }
            await ReplyAsync(EmbedHandler.CreateSimpleEmbed("Announce", $"Message sent to {count} users", Color.Green));
        }

        [Command("Reset")]
        [Summary("Reset server settings to their default values")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task Reset()
        {
            await Guilds.ResetAsync(Context.Guild);
            await ReplyAsync(EmbedHandler.CreateSimpleEmbed("Reset", "Succesfully Reset Server Settings 🔃", Color.Green));
        }

        [Command("Update")]
        [Summary("Update all server documents to the latest version")]
        [RequireOwner]
        public async Task Update()
        {
            int count = 0;
            var socketGuilds = Context.Client.Guilds;
            foreach (var socketGuild in socketGuilds)
            {
                try
                {
                    var guild = await Guilds.GetAsync(socketGuild);
                    await Guilds.Save(guild);
                    count++;           
                }
                catch {}
            }
            await ReplyAsync(await EmbedHandler.CreateSimpleEmbed("Update", $":robot: Updated {count} servers to the latest version!", Color.Purple));
        }
    }
}