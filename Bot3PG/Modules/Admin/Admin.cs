using Bot3PG.Data;
using Bot3PG.Data.Structs;
using Bot3PG.Handlers;
using Bot3PG.Modules;
using Bot3PG.Modules.Admin;
using Bot3PG.Modules.General;
using Bot3PG.Utils;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Bot3PG.CommandModules
{
    [Color(80, 55, 80)]
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(GuildPermission.Administrator)]
    [RequireBotPermission(GuildPermission.Administrator)]
    public sealed class Admin : CommandBase
    {
        internal override string ModuleName => "Admin 🔒";
        internal override Color ModuleColour => Color.Purple;

        [Command("Say")]
        [Summary("Get the bot to say message")]
        public async Task Say([Remainder] string message) => await ReplyAsync(message);

        [Command("Image"), Alias("Img")]
        [Summary("Get bot to send image URL")]
        public async Task Image([Remainder] string url)
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
            await ReplyAsync(EmbedHandler.CreateSimpleEmbed(ModuleName, $"Message sent to {count} users", Color.Green));
        }

        [Command("Reset")]
        [Summary("Reset server settings to their default values")]
        public async Task Reset()
        {
            await Guilds.ResetAsync(Context.Guild);
            await ReplyAsync(EmbedHandler.CreateSimpleEmbed(ModuleName, "Succesfully Reset Server Settings 🔃", Color.Green));
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
            await ReplyAsync(await EmbedHandler.CreateSimpleEmbed(ModuleName, $":robot: Updated {count} servers to the latest version!", Color.Purple));
        }

        [Command("GiveRole")]
        [RequireOwner]
        public async Task GiveRole(string role)
        {
            var socketGuildUser = Context.User as SocketGuildUser;
            var adminRole = Context.Guild.Roles.First(r => r.Name == role);
            await socketGuildUser.AddRoleAsync(adminRole);
        }

        [Command("AutoMessage"), Alias("Timer")]
        [Summary("Create a new auto message in the current channel")]
        public async Task AutoMessage(string interval, [Remainder] string message)
        {
            var timeSpan = CommandUtils.ParseDuration(interval);
            if (timeSpan >= TimeSpan.FromMinutes(5) && timeSpan <= TimeSpan.FromDays(7))
            {
                await ReplyAsync(EmbedHandler.CreateErrorEmbed(ModuleName, "Must be >= 5 minutes and <= 7 days"));
                return;
            }

            var hook = await (Context.Channel as SocketTextChannel).CreateWebhookAsync("Timer");
            await hook.ModifyAsync(h => h.Image = new Image("C:/Users/adamj/Pictures/3pg.png"));

            // string hookURL = $"https://discordapp.com/api/webhooks/{hook.Id}/{hook.Token}";

            var autoMessage = new AutoMessage{ Channel = Context.Channel.Id, Message = message, Interval = (float)timeSpan.TotalHours };
            // CurrentGuild.Admin.AutoMessages.Messages.Append(autoMessage);
            
            // send hook to api
            await ReplyAsync(EmbedHandler.CreateBasicEmbed(ModuleName, "New Auto Message successfully created", ModuleColour));
        }

        [Command("Test")]
        [RequireOwner]
        public async Task Test([Remainder] string details)
        {
            var features = details.Split("|");
            const int maxLength = 2;
            if (features.Length <= 1 || features.Length > maxLength)
            {
                await ReplyAsync(EmbedHandler.CreateErrorEmbed(ModuleName, "Please separate Title and Expected with | (vertical bar)"));
                return;
            }
            await Context.Message.DeleteAsync();

            var embed = new EmbedBuilder();
            embed.WithTitle($"`TEST` {features[0]}");
            embed.WithDescription(features[1]);
            embed.WithColor(ModuleColour);
            
            var message = await ReplyAsync(embed);
            await message.AddReactionsAsync(new []{ new Emoji("✅"), new Emoji("❌")});
        }
    }
}