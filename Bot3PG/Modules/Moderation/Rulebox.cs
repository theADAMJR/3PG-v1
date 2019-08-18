using Bot3PG.Core.Data;
using Discord;
using Discord.WebSocket;
using System.Linq;
using System.Threading.Tasks;

namespace Bot3PG.Modules.Moderation
{
    public class Rulebox
    {
        public async Task AgreedToRules(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var socketGuildUser = reaction.User.Value as SocketGuildUser;
            if (socketGuildUser is null || socketGuildUser.IsBot) return;
            var guild = await Guilds.GetAsync(socketGuildUser.Guild);
            System.Console.WriteLine(guild.Config.RuleboxMessage.Id);
            if (reaction.Message.Value != guild.Config.RuleboxMessage/* && reaction.Message.Value.Id != guild.Config.VoteboxMessageID*/) return;
            /*if (reaction.Message.Value.Id == guild.Config.VoteboxMessageID)
            {
                Console.WriteLine("OnReactionAdded - voteboxmsgid");
                var voteEmotes = new IEmote[] { new Emoji("🇦"), new Emoji("🇧"), new Emoji("🇨"), new Emoji("🇩"), new Emoji("🇪"), new Emoji("🇫") };
                
                if (voteEmotes.Contains(reaction.Emote))
                {
                    await reaction.Message.Value.ModifyAsync(msg =>
                    {
                        msg.Content = "";
                        msg.Embed = new EmbedBuilder()
                            .WithColor(Color.Teal)
                            .WithTitle("Title")
                            .WithDescription("Description")
                            .Build();
                    });
                }
            }*/

            var user = await Users.GetAsync(socketGuildUser);
            // TODO - add emoji config
            string agreeEmote = "✅";
            string disagreeEmote = "❌";
            System.Console.WriteLine("2");

            if (reaction.Emote.Name == agreeEmote)
            {
                // TODO - move to user.AgreeAsync();
                user.Status.AgreedToRules = true;
                ;
                await socketGuildUser.AddRoleAsync(guild.Config.AgreeRole);
            }
            else if (reaction.Emote.Name == disagreeEmote)
            {
                // TODO - move to user.DisagreeAsync();
                user.Status.AgreedToRules = false;

                var roles = socketGuildUser.Roles.ToList();
                roles.RemoveAt(0);

                await socketGuildUser.RemoveRolesAsync(roles);
                await user.KickAsync($"You must agree to the rules.");
            }
        }

        public async Task OnReactionRemoved(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var socketGuildUser = reaction.User.Value as SocketGuildUser;
            var user = await Users.GetAsync(socketGuildUser);
            var guild = await Guilds.GetAsync(socketGuildUser.Guild);

            var agreeEmote = new Emoji("✅") as IEmote;

            if (reaction.Message.Value == guild.Config.RuleboxMessage)
            {
                if (reaction.Emote == agreeEmote && !socketGuildUser.IsBot)
                {
                    user.Status.AgreedToRules = false;

                    var roles = socketGuildUser.Roles.ToList();
                    roles.RemoveAt(0);
                    await socketGuildUser.RemoveRolesAsync(roles);
                }
            }
        }

        public async Task RemoveUserReaction(SocketGuildUser socketGuildUser)
        {
            var agreeEmote = new Emoji("✅") as IEmote;

            var guild = await Guilds.GetAsync(socketGuildUser.Guild);
            if (guild.Config.RuleboxMessage is null) return;
            await guild.Config.RuleboxMessage.RemoveReactionAsync(agreeEmote, socketGuildUser);
        }
    }
}