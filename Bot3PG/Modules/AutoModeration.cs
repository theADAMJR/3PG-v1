using Bot3PG.Core.LevelingSystem;
using Bot3PG.Core.Users;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Bot3PG.Modules
{
    public class AutoModeration
    {
        public async Task OnMessageRecieved(SocketMessage msg)
        {
            bool validMessage = IsMessageValid(msg.Content);
            if (!validMessage)
            {
                await WarnUser(msg.Author as SocketGuildUser, "Explicit message", false);
                await msg.DeleteAsync();
                if (msg.Author != null)
                    ExtendCooldown(msg.Author as SocketGuildUser);
            }
            return;
        }
        public static bool IsMessageValid(string msgContents)
        {
            var banWords = BannedWords.GetWords();
            var banLinks = BannedLinks.GetLinks();
            var upperCaseMsg = msgContents.ToLower();

            foreach (string badWord in banWords)
            {
                if (upperCaseMsg.Contains(badWord.ToLower()))
                    return false;
            }
            foreach (string badLink in banLinks)
            {
                if (upperCaseMsg.Contains(badLink.ToLower()))
                    return false;
            }
            return true;
        }
        public void ExtendCooldown(SocketGuildUser user)
        {
            var userAccount = Accounts.GetAccount(user);
            userAccount.LastXPMsg = DateTime.Now.Add(TimeSpan.FromSeconds(Global.Config.ExtendedXPCooldown * userAccount.NumberOfWarnings));
        }

        public static async Task WarnUser(SocketGuildUser user, string reason, bool muteUser)
        {
            if (user.GuildPermissions.Administrator) return;

            Accounts.GetAccount(user).NumberOfWarnings++;
            var account = Accounts.GetAccount(user);
            var warnings = Accounts.GetAccount(user).NumberOfWarnings;
            Accounts.SaveAccounts();

            var warningBanDisabled = Global.Config.WarningNumberToBan == -1; // '-1' disables warning auto ban
            var warningKickDisabled = Global.Config.WarningNumberToBan == -1;

            //if (muteUser)
            //    await RepeatingTimer.StartMuteTimer(user as SocketGuildUser);

            if (warnings >= Global.Config.WarningNumberToBan && !warningBanDisabled)
            {
                await user.SendMessageAsync($"Warning #{warnings}, you have been banned from {user.Guild.Name} - '{reason}'");
                await user.Guild.AddBanAsync(user, 0, $"Warning #{warnings} - ban"); // TODO - enable config
            }
            else if (warnings >= Global.Config.WarningNumberToKick && !warningKickDisabled)
            {
                await user.SendMessageAsync($"Warning #{warnings}, you have been kicked from {user.Guild.Name} - '{reason}'");
                await user.KickAsync($"Warning #{warnings} - kick");
            }
            else
            {
                await user.SendMessageAsync($"Warning #{warnings}, you have been warned from {user.Guild.Name} - '{reason}'");
            }
        }
    }
}