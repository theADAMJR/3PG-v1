using Bot3PG.Core;
using Bot3PG.Core.Users;
using Bot3PG.Modules;
using Bot3PG.Services;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Victoria;

namespace Bot3PG.Modules
{
    public class Rulebox
    {
        public async Task OnReactionAdded(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (reaction.Message.Value.Id != Global.RuleboxMessageID && reaction.Message.Value.Id != Global.VoteboxMessageID) return;
            if (reaction.Message.Value.Id == Global.VoteboxMessageID)
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
            }


            bool userAgreedToRules = Accounts.GetAccount(reaction.User.Value as SocketGuildUser).AgreedToRules;
            var agreeEmote = new Emoji("✅") as IEmote;
            var disagreeEmote = new Emoji("❌") as IEmote;

            var ruleBox = cache.Value;
            var user = (reaction.User.Value as SocketGuildUser);
            if (reaction.MessageId == Global.RuleboxMessageID)
            {
                if (reaction.Emote.Name == "✅" && !user.IsBot)
                {
                    Accounts.GetAccount(reaction.User.Value as SocketGuildUser).AgreedToRules = true;
                    Accounts.SaveAccounts();

                    var role = user.Guild.Roles.FirstOrDefault(x => x.Name == Global.Config.AgreeRoleName);
                    await user.AddRoleAsync(role);
                }
                else if (reaction.Emote.Name == "❌" && !user.IsBot)
                {
                    Accounts.GetAccount(reaction.User.Value as SocketGuildUser).AgreedToRules = false;
                    Accounts.SaveAccounts();

                    var roles = user.Roles.ToList();
                    var defaultChannel = (reaction.User.Value as IGuildUser).Guild.GetDefaultChannelAsync();
                    roles.RemoveAt(0);

                    await user.RemoveRolesAsync(roles);
                    await user.KickAsync($"Kicked from {user.Guild.Name} - You must agree to the rules.");
                }
            }
        }

        public async Task OnReactionRemoved(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var user = (reaction.User.Value as SocketGuildUser);
            if (reaction.MessageId == Global.RuleboxMessageID)
            {
                if (reaction.Emote.Name == "✅" && !user.IsBot)
                {
                    Accounts.GetAccount(reaction.User.Value as SocketGuildUser).AgreedToRules = false;
                    Accounts.SaveAccounts();

                    var roles = user.Roles.ToList();
                    roles.RemoveAt(0);
                    await user.RemoveRolesAsync(roles);
                }
            }
        }
    }
}