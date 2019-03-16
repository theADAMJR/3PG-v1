using System;
using System.Collections.Generic;
using System.Text;

namespace Bot3PG.DataStructs
{
    public class Guild
    {
        public ulong ID;

        public struct Options
        {
            public bool AnnounceEnabled;
            public ulong AnnounceChannelID;
            public bool XPBot;
            public uint XPPerMessage;
            public uint XPMessageLengthThreshold;
            public uint XPMessageLengthThresholdMute;
            public uint XPCooldown;
            public uint ExtendedXPCooldown;
            public uint MessageSpamThreshold;
            public uint MessageSpamThresholdMute;
            public uint AutoMuteSeconds;
            public int LeaderboardSize;
            public int MaxLeaderboardPage;
            public bool AutoModerationEnabled;
            public bool ModCommandsEnabled;
            public int WarningNumberToKick;
            public int WarningNumberToBan;
            public string AgreeRoleName;
            public ulong RuleboxMessageID;
            public ulong StaffLogChannelID;
        }
    }
}