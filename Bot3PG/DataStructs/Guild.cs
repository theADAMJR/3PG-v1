using Bot3PG.DataStructs.Attributes;
using Bot3PG.Modules;
using Discord;
using Discord.WebSocket;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using System.Linq;

namespace Bot3PG.DataStructs
{
    public class Guild
    {
        private static ulong _id;
        [NotConfigurable]
        [BsonId] public ulong ID { get => _id; set => _id = value; }

        [NotConfigurable]
        public bool IsPremium { get; private set; }

        public bool IsDisabled { get; set; }

        [NotConfigurable]
        public AdminModule Admin { get; set; } = new AdminModule();
        [NotConfigurable]
        public GeneralModule General { get; set; } = new GeneralModule();
        [NotConfigurable]
        public ModerationModule Moderation { get; set; } = new ModerationModule();
        [NotConfigurable]
        public MusicModule Music { get; set; } = new MusicModule();
        [NotConfigurable]
        public XPModule XP { get; set; } = new XPModule();

        [BsonIgnore] public static SocketGuild DiscordGuild => Global.Client.GetGuild(_id);

        public Guild(SocketGuild socketGuild) => ID = socketGuild.Id;

        [Description("Features for server admins")]
        public class AdminModule : ConfigModule
        {
            [NotConfigurable]
            public RuleboxSubModule Rulebox { get; set; } = new RuleboxSubModule();

            [Description("Make members have to agree to the rules to use your server"), Release(Release.Unstable)]
            public class RuleboxSubModule : SubModule
            {
                private ulong channelID;
                // TODO - make readonly
                [Description("The channel containing the rulebox message")]
                [BsonIgnore] public SocketTextChannel RuleboxChannel { get => DiscordGuild.GetTextChannel(channelID); set => channelID = value.Id; }

                private ulong ruleboxMessageId;
                [Description("The rulebox message")]
                [BsonIgnore] public IUserMessage Message { get => RuleboxChannel.GetCachedMessage(ruleboxMessageId) as IUserMessage; set => ruleboxMessageId = value.Id; }

                private ulong agreeRoleId;
                [Description("The role given to members that agree to the rules")]
                [BsonIgnore] public SocketRole AgreeRole { get => DiscordGuild.GetRole(agreeRoleId); set => agreeRoleId = value.Id; }
            }

            //public ulong VoteboxMessageID { get; set; }
        }

        [Description("General features")]
        public class GeneralModule : ConfigModule
        {
            public AnnounceSubModule Announce { get; set; } = new AnnounceSubModule();

            [Description("The character that is typed before commands")]
            public string CommandPrefix { get; set; } = "/";

            public class AnnounceSubModule : SubModule
            {
                [Description("Welcome messages for new users")]
                [ExtraInfo("**Placeholders:** `[GUILD]` or `[SERVER]` - Discord server name\n `[USER]` - Mention target server user\n")]
                public List<string> WelcomeMessages { get; set; } = new List<string> { "Welcome to [GUILD], [USER]", "Welcome [USER], to [GUILD]", "Hey [USER]! Welcome to [GUILD]" };

                [Description("Goodbye messages for users")]
                public List<string> GoodbyeMessages { get; set; } = new List<string> { "[USER] left the server.", "Sad to see you [USER].", "Bye [USER]!" };

                private ulong announceChannelId;
                [Description("Channel for server welcome announcements")]
                [BsonIgnore] public SocketTextChannel Channel { get => DiscordGuild.GetTextChannel(announceChannelId); set => announceChannelId = value.Id; }
            }
        }

        [Description("Control your server")]
        public class ModerationModule : ConfigModule
        {
            public AutoModerationSubModule Auto { get; set; } = new AutoModerationSubModule();
            public StaffLogsSubModule StaffLogs { get; set; } = new StaffLogsSubModule();

            [Description("Allow 3PG to punish offenders!")]
            public class AutoModerationSubModule : SubModule
            {
                [Description("Use a list of predefined explicit words for auto detection")]
                public bool UseDefaultBanWords { get; set; } = true;

                [Description("Use a list of predefined explicit links for auto detection")]
                public bool UseDefaultBanLinks { get; set; } = true;

                [Description("Use your own or additional ban words")]
                public List<string> CustomBanWords { get; set; } = new List<string>();

                [Description("Use your own or additional ban links")]
                public List<string> CustomBanLinks { get; set; } = new List<string>();

                [Description("Warnings required to auto-kick offender")]
                public int WarningsForKick { get; set; } = 5;

                [Description("Warnings required to auto-ban offender")]
                public int WarningsForBan { get; set; } = 10;

                [Description("Length of time to auto-mute offender")]
                public int AutoMuteSeconds { get; set; } = 60;

                [Description("Prevent user access if they have an explicit username/nickname")]
                public bool NicknameFilter { get; set; } = true;
            }

            [Description("Allow logging of user's actions")]
            public class StaffLogsSubModule : SubModule
            {
                private ulong channelId;
                [Description("Channel for logs")]
                [BsonIgnore] public SocketTextChannel Channel { get => DiscordGuild.GetTextChannel(channelId); set => channelId = value.Id; }

                // TODO: enum for enabled log types
            }
        }

        [Description("Music features"), Release(Release.Alpha)]
        public class MusicModule : ConfigModule
        {
            [Description("Default volume for music")]
            public int DefaultVolume { get; set; } = 100;
        }

        [Description("Earn EXP and reward user's activity")]
        public class XPModule : ConfigModule
        {
            public RoleRewardsSubModule RoleRewards { get; set; } = new RoleRewardsSubModule();

            [Description("The amount of EXP each message receives")]
            public int EXPPerMessage { get; set; } = 50;

            [Description("Minimum character length for a message to earn EXP")]
            public int MessageLengthThreshold { get; set; } = 3;

            [Description("How long the user has to wait to earn EXP again")]
            public int Cooldown { get; set; } = 5;

            [Premium, Description("The maximum amount of pages for the leaderboard")]
            public int MaxLeaderboardPage { get; set; } = 100;
            
            [Premium, Description("A cooldown given to users after being muted")]
            public int ExtendedCooldown { get; set; } = 300;

            // xp exempt channels
            // xp exempt roles

            [Description("Reward roles as XP rewards"), Release(Release.Alpha)]
            public class RoleRewardsSubModule : SubModule
            {
                public SocketRole this[uint levelNumber]
                {
                    get => levelRoleIds.Select(id => DiscordGuild.GetRole(id.Value)).FirstOrDefault();
                    set => levelRoleIds[levelNumber.ToString()] = value.Id;
                }

                public bool RolesExist => levelRoleIds.Count > 0;

                [Description("Whether old XP roles should be removed after one is added")]
                public bool StackRoles { get; set; } = true;

                [BsonRequired] private Dictionary<string, ulong> levelRoleIds = new Dictionary<string, ulong>();
            }
        }
    }
}