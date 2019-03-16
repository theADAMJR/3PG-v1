using Bot3PG.Core.LevelingSystem;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Bot3PG.Core
{
    internal static class RepeatingTimer
    {
        private static Timer loopingTimer;

        internal static Task StartMuteTimer(SocketGuildUser user)
        {
            var account = Users.Accounts.GetAccount(user);
            account.IsMuted = true;

            loopingTimer = new Timer()
            {
                Interval = (Global.Config.AutoMuteSeconds * 100) * account.NumberOfWarnings,
                AutoReset = false,
                Enabled = true
            };
            //loopingTimer.Elapsed += UnmuteUser;

            return Task.CompletedTask;
        }

        //private static async void UnmuteUser(object sender, ElapsedEventArgs e)
        //{
        //    Users.UserAccounts.GetAccount(sender as SocketGuildUser).IsMuted = false;
        //}
    }
}