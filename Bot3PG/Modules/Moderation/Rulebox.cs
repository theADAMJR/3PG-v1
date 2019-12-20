using Bot3PG.Data;
using Bot3PG.Data.Structs;
using Bot3PG.Handlers;
using Discord;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Bot3PG.Modules.Moderation
{
    public static class Rulebox
    {
        public static async Task CheckRuleAgreement(Guild guild, SocketGuildUser socketGuildUser, SocketReaction reaction)
        {
            try
            {
                if (socketGuildUser is null || socketGuildUser.IsBot) return;
                if (reaction.MessageId != guild.Admin.Rulebox.MessageId) return;

                var user = await Users.GetAsync(socketGuildUser);
                if (reaction.Emote.Name == guild.Admin.Rulebox.AgreeEmote)
                {
                    var role = socketGuildUser.Guild.Roles.First(r => r.Id == guild.Admin.Rulebox.Role);
                    await socketGuildUser.AddRoleAsync(role);
                    user.Status.AgreedToRules = true;
                }
                else if (reaction.Emote.Name == guild.Admin.Rulebox.DisagreeEmote)
                {
                    user.Status.AgreedToRules = false;

                    var roles = socketGuildUser.Roles.ToList();
                    roles.RemoveAt(0);

                    var bot = socketGuildUser.Guild.GetUser(Global.Client.CurrentUser.Id);
                    if (socketGuildUser.Hierarchy <= bot.Hierarchy)
                    {
                        await socketGuildUser.RemoveRolesAsync(roles); 
                        await user.KickAsync($"Please agree to the rules to use `{socketGuildUser.Guild.Name}`.", Global.Client.CurrentUser);
                    }
                }
                await Users.Save(user);
            }
            catch (Exception ex) { await reaction.Channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Rulebox", ex.Message)); }
        }

        public static async Task OnReactionRemoved(Cacheable<IUserMessage, ulong> before, ISocketMessageChannel channel, SocketReaction reaction)
        {
            try
            {
                var socketGuildUser = reaction.User.Value as SocketGuildUser;
                var user = await Users.GetAsync(socketGuildUser);
                var guild = await Guilds.GetAsync(socketGuildUser.Guild);

                if (!socketGuildUser.IsBot && reaction.MessageId == guild.Admin.Rulebox.MessageId && reaction.Emote.Name == guild.Admin.Rulebox.AgreeEmote)
                {
                    var roles = socketGuildUser.Roles.ToList();
                    roles.RemoveAt(0);
                    await socketGuildUser.RemoveRolesAsync(roles);
                    user.Status.AgreedToRules = false;
                }
                await Users.Save(user);                
            }
            catch (Exception ex) { await channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Rulebox", ex.Message)); }
        }

        public static async Task RemoveUserReaction(SocketGuildUser socketGuildUser)
        {
            try
            {
                var guild = await Guilds.GetAsync(socketGuildUser.Guild);
                var rulebox = guild.Admin.Rulebox;

                var agreeEmote = new Emoji(rulebox.AgreeEmote) as IEmote;
                if (rulebox.MessageId != 0)
                {
                    var message = await socketGuildUser.Guild.GetTextChannel(rulebox.Channel)?.GetMessageAsync(rulebox.MessageId) as IUserMessage;
                    if (message is null) return;

                    await message.RemoveReactionAsync(agreeEmote, message.Author);
                }                
            }
            catch (Exception ex) { await socketGuildUser.Guild.DefaultChannel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Rulebox", ex.Message)); }
        }
    }
}