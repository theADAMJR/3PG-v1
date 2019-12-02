using Bot3PG.Core.Data;
using Bot3PG.DataStructs;
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
        public static async Task AgreedToRules(Guild guild, SocketGuildUser socketGuildUser, SocketReaction reaction)
        {
            try
            {
                if (socketGuildUser is null || socketGuildUser.IsBot) return;
                if (reaction.MessageId != guild.Admin.Rulebox.Id) return;

                var user = await Users.GetAsync(socketGuildUser);
                // TODO - add emoji config
                string agreeEmote = "✅";
                string disagreeEmote = "❌";

                if (reaction.Emote.Name == agreeEmote)
                {
                    var role = socketGuildUser.Guild.Roles.First(r => r.Id == guild.Admin.Rulebox.RoleId);
                    await socketGuildUser.AddRoleAsync(role);
                    user.Status.AgreedToRules = true;
                }
                else if (reaction.Emote.Name == disagreeEmote)
                {
                    user.Status.AgreedToRules = false;

                    var roles = socketGuildUser.Roles.ToList();
                    roles.RemoveAt(0);

                    await socketGuildUser.RemoveRolesAsync(roles);
                    await user.KickAsync($"Please agree to the rules to use `{socketGuildUser.Guild.Name}`.", Global.Client.CurrentUser);
                }
                await Users.Save(user);
            }
            catch (Exception ex)
            {
                await reaction.Channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Rulebox", ex.Message));
            }
        }

        public static async Task OnReactionRemoved(Cacheable<IUserMessage, ulong> before, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var socketGuildUser = reaction.User.Value as SocketGuildUser;
            var user = await Users.GetAsync(socketGuildUser);
            var guild = await Guilds.GetAsync(socketGuildUser.Guild);

            string agreeEmote = "✅";
            if (!socketGuildUser.IsBot && reaction.MessageId == guild.Admin.Rulebox.Id && reaction.Emote.Name == agreeEmote)
            {
                var roles = socketGuildUser.Roles.ToList();
                roles.RemoveAt(0);
                await socketGuildUser.RemoveRolesAsync(roles);
                user.Status.AgreedToRules = false;
            }
            await Users.Save(user);
        }

        public static async Task RemoveUserReaction(SocketGuildUser socketGuildUser)
        {
            var agreeEmote = new Emoji("✅") as IEmote;

            var guild = await Guilds.GetAsync(socketGuildUser.Guild);
            var rulebox = guild.Admin.Rulebox;
            if (rulebox.Id != 0)
            {
                var message = await socketGuildUser.Guild.GetTextChannel(rulebox.ChannelId).GetMessageAsync(guild.Admin.Rulebox.Id) as IUserMessage;
                await message.RemoveReactionAsync(agreeEmote, message.Author);
            }
        }
    }
}