using Discord;
using Discord.WebSocket;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Bot3PG.DataStructs
{
    public class Guild
    {
        private static ulong _id;
        [BsonId]
        public ulong ID
        {
            get => _id;
            set => _id = value;
        }

        public Options Config;

        [BsonIgnore]
        public static SocketGuild DiscordGuild => Global.Client.GetGuild(_id);
        public Guild(SocketGuild socketGuild)
        {
            ID = socketGuild.Id;
            Config = new Options();
        }

        public class Options
        {
            // TODO - add seperate structs for guild modules

            // TODO - add string[] welcomeMessage
            // TODO - add string[] goodbyeMessage
            // TODO - add bool randomizeAnnouncements
            // TODO - add string[] banWords

            public string[] BanWords = BannedWords.GetWords();

            /*public bool RandomizeAnnouncements
            {
                get => (bool)Select(guildConfigTable, "randomize_announcements");
                set => Update(guildConfigTable, "randomize_announcements", value);
            }*/

            public string[] WelcomeMessages { get; set; } = new string[3] { "Welcome to [GUILD], [USER]", "Welcome [USER], to [GUILD]", "Hey [USER]! Welcome to [GUILD]" };

            public string[] GoodbyeMessages { get; set; } = new string[3] { "[USER] left the server.", "Sad to see you [USER].", "Bye [USER]!" };

            [Description("The character that is typed before commands")]
            public string CommandPrefix { get; set; } = "/";
            public bool AnnounceEnabled { get; set; } = true;

            private ulong announceChannelId;
            [BsonIgnore]
            public SocketTextChannel AnnounceChannel
            {
                get => DiscordGuild.GetTextChannel(announceChannelId);
                set => announceChannelId = value.Id;
            }

            public bool MusicEnabled { get; set; } = true;
            public int DefaultVolume { get; set; } = 100;
            public bool AutoModerationEnabled { get; set; } = true;
            public int WarningsForKick { get; set; } = 5;
            public int WarningsForBan { get; set; } = 10;
            public int AutoMuteSeconds { get; set; } = 60;
            public bool ModerationEnabled { get; set; } = true;
            public bool RuleboxEnabled { get; set; } = true;

            private ulong agreeRoleId;
            [BsonIgnore]
            public SocketRole AgreeRole
            {
                get => DiscordGuild.GetRole(agreeRoleId);
                set => agreeRoleId = value.Id;
            }

            private ulong ruleboxChannelId;
            [BsonIgnore]
            public SocketTextChannel RuleboxChannel
            {
                get => DiscordGuild.GetTextChannel(ruleboxChannelId);
                set => ruleboxChannelId = value.Id;
            }

            private ulong ruleboxMessageId;
            [BsonIgnore]
            public IUserMessage RuleboxMessage
            {
                get => RuleboxChannel.GetCachedMessage(ruleboxMessageId) as IUserMessage;
                set => ruleboxMessageId = value.Id;
            }

            public bool StaffLogsEnabled { get; set; }

            private ulong staffLogsChannelId;
            [BsonIgnore]
            public SocketTextChannel StaffLogsChannel
            {
                get => DiscordGuild.GetTextChannel(staffLogsChannelId);
                set => staffLogsChannelId = value.Id;
            }

            public bool XPEnabled { get; set; }
            public int XPPerMessage { get; set; }
            public int XPMessageLengthThreshold { get; set; }
            public int XPCooldown { get; set; }
            public int ExtendedXPCooldown { get; set; }
            public int MaxLeaderboardPage { get; set; }
            public ulong VoteboxMessageID { get; set; }
        }
    }
}