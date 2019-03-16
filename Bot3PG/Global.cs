using Bot3PG.DataStructs;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot3PG
{
    public static class Global
    {
        public static DiscordSocketClient Client { get; set; }
        public static ulong RuleboxMessageID { get; set; }
        public static ulong VoteboxMessageID { get; set; }
        public static Config Config { get; set; }

        private static DateTime _startTime;
        public static TimeSpan Uptime
        {
            get
            {
                var duration = DateTime.Now - _startTime;
                return duration;
            }
        }

        public static void InitializeStartTime()
        {
            _startTime = DateTime.Now;
        }
    }
}