using Discord;
using Discord.WebSocket;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

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

        public bool IsPremium { get; private set; }

        public AdminModule Admin = new AdminModule();
        public GeneralModule General = new GeneralModule();
        public ModerationModule Moderation = new ModerationModule();
        public MusicModule Music = new MusicModule();
        public XPModule XP = new XPModule();

        [BsonIgnore]
        public static SocketGuild DiscordGuild => Global.Client.GetGuild(_id);

        public Guild(SocketGuild socketGuild) => ID = socketGuild.Id;

        public class AdminModule : ConfigModule
        {
            public RuleboxSubModule Rulebox = new RuleboxSubModule();

            public class RuleboxSubModule : SubModule
            {
                public bool Enabled { get; set; } = true;

                private ulong ChannelID;
                [BsonIgnore]
                public SocketTextChannel RuleboxChannel
                {
                    get => DiscordGuild.GetTextChannel(ChannelID);
                    set => ChannelID = value.Id;
                }

                private ulong ruleboxMessageId;
                [BsonIgnore]
                public IUserMessage RuleboxMessage
                {
                    get => RuleboxChannel.GetCachedMessage(ruleboxMessageId) as IUserMessage;
                    set => ruleboxMessageId = value.Id;
                }

                private ulong agreeRoleId;
                [BsonIgnore]
                public SocketRole AgreeRole
                {
                    get => DiscordGuild.GetRole(agreeRoleId);
                    set => agreeRoleId = value.Id;
                }
            }

            public ulong VoteboxMessageID { get; set; }
        }

        public class GeneralModule : ConfigModule
        {
            public AnnounceSubModule Announce = new AnnounceSubModule();

            [Description("The character that is typed before commands")]
            public string CommandPrefix { get; set; } = "/";

            public class AnnounceSubModule : SubModule
            {
                public bool Enabled { get; set; } = true;
                public string[] WelcomeMessages { get; set; } = new string[3] { "Welcome to [GUILD], [USER]", "Welcome [USER], to [GUILD]", "Hey [USER]! Welcome to [GUILD]" };
                public string[] GoodbyeMessages { get; set; } = new string[3] { "[USER] left the server.", "Sad to see you [USER].", "Bye [USER]!" };

                private ulong announceChannelId;
                [BsonIgnore]
                public SocketTextChannel AnnounceChannel { get => DiscordGuild.GetTextChannel(announceChannelId); set => announceChannelId = value.Id; }
            }
        }

        public class ModerationModule : ConfigModule
        {
            public bool Enabled { get; set; } = true;

            public AutoModerationSubModule AutoModeration = new AutoModerationSubModule();
            public StaffLogsSubModule StaffLogs = new StaffLogsSubModule();

            public class AutoModerationSubModule : SubModule
            {
                public bool Enabled { get; set; } = true;
                public bool UseDefaultBanWords { get; set; } = true;
                public string[] CustomBanWords { get; set; }
                public int WarningsForKick { get; set; } = 5;
                public int WarningsForBan { get; set; } = 10;
                public int AutoMuteSeconds { get; set; } = 60;
            }

            public class StaffLogsSubModule : SubModule
            {
                public bool Enabled { get; set; } = true;

                private ulong channelId;
                [BsonIgnore]
                public SocketTextChannel Channel { get => DiscordGuild.GetTextChannel(channelId); set => channelId = value.Id; }
            }
        }

        public class MusicModule : ConfigModule
        {
            public bool Enabled { get; set; } = true;
            public int DefaultVolume { get; set; } = 100;

        }

        public class XPModule : ConfigModule
        {
            public bool Enabled { get; set; } = true;
            public int EXPPerMessage { get; set; } = 50;
            public int MessageLengthThreshold { get; set; } = 3;
            public int Cooldown { get; set; } = 60;

            [Premium]
            public int MaxLeaderboardPage { get; set; }

            public int ExtendedXPCooldown { get; set; }
        }

        // TODO - remove
        public class Options
        {
            // TODO - add seperate structs for guild modules

            // TODO - add string[] welcomeMessage
            // TODO - add string[] goodbyeMessage
            // TODO - add bool randomizeAnnouncements
            // TODO - add string[] banWords

            /*public bool RandomizeAnnouncements
            {
                get => (bool)Select(guildConfigTable, "randomize_announcements");
                set => Update(guildConfigTable, "randomize_announcements", value);
            }*/
        }
    }
}