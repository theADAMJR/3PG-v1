using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bot3PG.Core;
using Bot3PG.Core.Users;
using Bot3PG.DataStructs;

namespace Bot3PG.Core.LevelingSystem
{
    public class Leveling
    {
        public static async void ValidateMessageForXP(SocketUserMessage msg)
        {
            if (msg == null) return;
            var user = msg.Author as SocketGuildUser;
            var guild = (msg.Author as SocketGuildUser).Guild;
            var userAccount = Accounts.GetAccount(user);
            
            if (XPCooldownActive(userAccount) || msg.Content.Length <= Global.Config.XPMessageLengthThreshold) return;
            
            else
            {
                userAccount.LastXPMsg = DateTime.Now;

                uint oldLevel = userAccount.LevelNumber;
                userAccount.XP += Global.Config.XPPerMessage;
                Accounts.SaveAccounts();
                uint newLevel = userAccount.LevelNumber;


                if (oldLevel != newLevel)
                {
                    // the user leveled up
                    var embed = new EmbedBuilder();
                    embed.WithColor(Color.Green);
                    embed.WithTitle("✨ **LEVEL UP!**");
                    embed.WithDescription(user.Mention + " just leveled up!");
                    embed.AddField("LEVEL", newLevel, true);
                    embed.AddField("XP", userAccount.XP, true);

                    await msg.Channel.SendMessageAsync("", embed: embed.Build());
                }
            }
        }
        public static bool XPCooldownActive(Account account)
        {
            if (account == null) return false;
            var lastMessageTime = DateTime.Now.Subtract(account.LastXPMsg);
            if (lastMessageTime.TotalSeconds <= Global.Config.XPCooldown)
            {
                return true;
            }
            return false;
        }

        /*internal static async void HandleSpam(SocketTextChannel channel, SocketGuildUser user)
        {
            var userAccount = UserAccounts.GetAccount(user);
            var secondsSinceLastMsg = DateTime.Now.Subtract(userAccount.LastMsg).TotalSeconds;

            if (secondsSinceLastMsg >= Config.bot.LastMsg)
            {
                return false;
            }
            if (userAccount.MessagesInLastMinute >= Config.bot.messageSpamThresholdMute)
            {
                await Program.WarnUser(user, "Spam", muteUser: true);
            }
        }*/
    }
}