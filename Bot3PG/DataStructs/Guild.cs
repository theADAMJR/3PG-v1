using Bot3PG.Core.Data;
using Bot3PG.DataStructs.Attributes;
using Bot3PG.Modules;
using Discord;
using Discord.WebSocket;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bot3PG.DataStructs
{
    public class Guild
    {
        [BsonIgnore] private static ulong _id;

        [BsonRepresentation(BsonType.String)]
        [BsonId] public ulong ID { get; private set; }

        public bool IsPremium { get; private set; }

        [Config("Features for admins")]
        public AdminModule Admin { get; private set; } = new AdminModule();

        [Config("Configure your server's dashboard")]
        public SettingsModule Settings { get; private set; } = new SettingsModule();

        [Config("General features")]
        public GeneralModule General { get; private set; } = new GeneralModule();

        [Config("Manage your server")]
        public ModerationModule Moderation { get; private set; } = new ModerationModule();

        [Config("Music features"), Release(Release.Alpha)]
        public MusicModule Music { get; private set; } = new MusicModule();

        [Config("Earn EXP and reward user's activity")]
        public XPModule XP { get; private set; } = new XPModule();

        [BsonIgnore] public static SocketGuild DiscordGuild => Global.Client.GetGuild(_id);

        public Guild(SocketGuild socketGuild)
        {
            _id = socketGuild.Id;
            ID = socketGuild.Id;
        }

        public class AdminModule : ConfigModule
        {
            [Config("Make members have to agree to the rules to use your server", release: Release.Alpha)]
            public RuleboxSubModule Rulebox { get; private set; } = new RuleboxSubModule();

            public class RuleboxSubModule : SubModule
            {
                [Config("The ID of the rulebox message")]
                [BsonRequired] public ulong Id { get; set; }

                [Config("The channel ID of the rulebox message")]
                [BsonRequired] public ulong ChannelId { get; set; }

                [Config("The ID of the role given to members that agree to the rules")]
                [BsonRequired] public ulong RoleId { get; set; }
            }
        }

        public class SettingsModule : ConfigModule
        {
            [Config("Set the minimum permissions for using members using webapp features")]
            public PermissionsSubModule Permissions { get; private set; } = new PermissionsSubModule();

            public class PermissionsSubModule : SubModule
            {
                [Config("Set minimum permission for editing server modules")]
                public string EditModules { get; set; }

                [Config("Required permission for viewing punishments")]
                public string ViewPunishments { get; set; }

                [Config("Set whether anyone can view your server's leaderboard, or only server members can view it")]
                public bool IsLeaderboardPublic { get; set; } = true;
            }
        }

        public class GeneralModule : ConfigModule
        {
            [Config("Send messages to users when they join or leave.")]
            public AnnounceSubModule Announce { get; private set; } = new AnnounceSubModule();

            [Config("The character that is typed before commands")]
            public string CommandPrefix { get; set; } = "/";

            [Config("Text channels that the bot ignores messages")]
            public ulong[] BlacklistedChannelIds { get; set; } = new ulong[] {};

            public class AnnounceSubModule : SubModule
            {
                [Config("Welcome messages for new users")]
                [ExtraInfo("**Placeholders:** `[GUILD]` or `[SERVER]` - Discord server name\n `[USER]` - Mention target server user\n")]
                public List<string> WelcomeMessages { get; set; } = new List<string> { "Welcome to [GUILD], [USER]", "Welcome [USER], to [GUILD]", "Hey [USER]! Welcome to [GUILD]" };

                [Config("Goodbye messages for users")]
                public List<string> GoodbyeMessages { get; set; } = new List<string> { "[USER] left the server.", "Sad to see you [USER].", "Bye [USER]!" };

                private ulong announceChannelId;
                [Config("Channel for server welcome announcements")]
                [BsonIgnore] public SocketTextChannel Channel { get => DiscordGuild.GetTextChannel(announceChannelId); set => announceChannelId = value.Id; }
            }
        }

        public class ModerationModule : ConfigModule
        {
            [Premium, Config("Allow 3PG to punish offenders!")]
            public AutoModerationSubModule Auto { get; private set; } = new AutoModerationSubModule();

            [Config("Allow logging of user's actions")]
            public StaffLogsSubModule StaffLogs { get; private set; } = new StaffLogsSubModule();

            [Config("Role automatically given to mute users")]
            public string MutedRoleName { get; private set; } = "Muted";

            public class AutoModerationSubModule : SubModule
            {
                [Config("Use a list of predefined explicit words for auto detection")]
                public bool UseDefaultBanWords { get; set; } = true;

                [Config("Maximum amount of messages can be sent in a minute by a user")]
                public int SpamThreshold { get; set; } = 60;

                [Config("Use a list of predefined explicit links for auto detection")]
                public bool UseDefaultBanLinks { get; set; } = true;

                [Config("Use your own or additional ban words")]
                public List<string> CustomBanWords { get; set; } = new List<string>();

                [Config("Use your own or additional ban links")]
                public List<string> CustomBanLinks { get; set; } = new List<string>();

                [Config("Warnings required to auto-kick offender")]
                public int WarningsForKick { get; set; } = 5;

                [Config("Warnings required to auto-ban offender")]
                public int WarningsForBan { get; set; } = 10;

                [Config("Length of time to auto-mute offender")]
                public int AutoMuteSeconds { get; set; } = 60;

                [Config("Prevent user access if they have an explicit username/nickname")]
                public bool NicknameFilter { get; set; } = true;
            }

            public class StaffLogsSubModule : SubModule
            {
                [BsonRequired, Config("Channel for logs")]
                public ulong ChannelId { get; set; }

                // TODO: enum for enabled log types
            }
        }

        public class MusicModule : ConfigModule
        {
            [Config("Default volume for music")]
            public int DefaultVolume { get; set; } = 100;
        }

        public class XPModule : ConfigModule
        {
            [Config("Reward roles as XP rewards"), Release(Release.Alpha)]
            public RoleRewardsSubModule RoleRewards { get; private set; } = new RoleRewardsSubModule();

            [Config("The amount of EXP each message receives")]
            public int EXPPerMessage { get; set; } = 50;

            [Config("Minimum character length for a message to earn EXP")]
            public int MessageLengthThreshold { get; set; } = 3;

            [Config("How long the user has to wait to earn EXP again")]
            public int Cooldown { get; set; } = 5;

            [Premium, Config("The maximum amount of pages for the leaderboard")]
            public int MaxLeaderboardPage { get; set; } = 100;
            
            [Premium, Config("A cooldown given to users after being muted")]
            public int ExtendedCooldown { get; set; } = 300;

            [Config("Delay when to allow messages with identical content to the last")]
            public TimeSpan DuplicateMessageThreshold { get; set; } = TimeSpan.FromSeconds(5);

            [Config("Text channels where EXP cannot be earned")]
            [BsonRepresentation(BsonType.String)]
            public List<ulong> ExemptChannelIds { get; set; } = new List<ulong>() { 123, 123 };
            
            [Premium, Config("Having any of these roles stops a user from earning EXP")]
            public List<ulong> ExemptRoleIds { get; set; } = new List<ulong>();

            public class RoleRewardsSubModule : SubModule
            {
                public SocketRole this[int levelNumber]
                {
                    get => levelRoleIds.Select(id => DiscordGuild.GetRole(id.Value)).FirstOrDefault();
                    set => levelRoleIds[levelNumber.ToString()] = value.Id;
                }

                public bool RolesExist => levelRoleIds.Count > 0;

                [Config("Whether old XP roles should be removed after one is added")]
                public bool StackRoles { get; set; } = true;

                [BsonRequired] private Dictionary<string, ulong> levelRoleIds = new Dictionary<string, ulong>();
            }
        }
    }
}