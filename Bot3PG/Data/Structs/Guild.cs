﻿using Bot3PG.Modules.Moderation;
using Discord;
using Discord.WebSocket;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using System.Linq;

namespace Bot3PG.Data.Structs
{
    public class Guild
    {
        [BsonIgnore] private static ulong _id;

        [BsonRepresentation(BsonType.String)]
        [BsonId] public ulong ID { get; private set; }

        public bool IsPremium { get; private set; }
        
        [Config("Features only for admins 🔒")]
        public AdminModule Admin { get; private set; } = new AdminModule();

        [Config("General features for general purposes")]
        public GeneralModule General { get; private set; } = new GeneralModule();

        [Config("Manage your server, or let 3PG do the job 🤖")]
        public ModerationModule Moderation { get; private set; } = new ModerationModule();

        [Config("Sit back and play any track 🎵")]
        public MusicModule Music { get; private set; } = new MusicModule();

        [Config("Earn EXP and reward user's activity ✨")]
        public XPModule XP { get; private set; } = new XPModule();

        [Config("Configure your server's dashboard ⚙")]
        public SettingsModule Settings { get; private set; } = new SettingsModule();

        [BsonIgnore] public static SocketGuild DiscordGuild => Global.Client.GetGuild(_id);

        public Guild(SocketGuild socketGuild) { _id = socketGuild.Id; ID = socketGuild.Id; }

        public class AdminModule : CommandConfigModule
        {
            [Config("Make members have to agree to the rules to use your server")]
            public RuleboxSubModule Rulebox { get; private set; } = new RuleboxSubModule();

            public class RuleboxSubModule : SubModule
            {
                [Config("The ID of the rulebox message")]
                [BsonRepresentation(BsonType.String)] public ulong MessageId { get; set; } // If a field was immutable before it updates (_id -> ...otherName) it will be required in saved schema

                [Config("The channel ID of the rulebox message"), SpecialType(typeof(SocketTextChannel))]
                [BsonRepresentation(BsonType.String)] public ulong Channel { get; set; }

                [Config("The ID of the role given to members that agree to the rules"), SpecialType(typeof(SocketRole))]
                [BsonRepresentation(BsonType.String)] public ulong Role { get; set; }

                [Config("Reaction emote to agree to the rules"), SpecialType(typeof(Emote))]
                public string AgreeEmote { get; set; } = "✅";

                [Config("Reaction emote to disagree to the rules"), SpecialType(typeof(Emote))]
                public string DisagreeEmote { get; set; } = "❌";
                
                [Config("Pin the rulebox automatically on creation")]
                public bool PinRulebox { get; set; } = true;

                [Config("Set the message in the rulebox embed")]
                public string Message { get; set; } = "Do you agree to the rules?";
            }
        }

        public class GeneralModule : CommandConfigModule
        {
            [Config("Send messages to users when they join or leave.", 
            extraInfo: "Variables: \n[NICKNAME] - user nickname \n[OWNER] - mention the server owner \n[USER] - mention the user \n"
            + "[USER_COUNT] - user count in server \n[USERNAME] - user nickname \n[SERVER] - server name")]
            public AnnounceSubModule Announce { get; private set; } = new AnnounceSubModule();

            [Config("The character that is typed before commands")]
            public string CommandPrefix { get; set; } = "/";

            [Config("Text channels that the bot ignores messages"), List(typeof(SocketTextChannel))]
            [BsonRepresentation(BsonType.String)]
            public ulong[] BlacklistedChannels { get; set; } = new ulong[] {};

            [Config("Upvote emote for suggestions"), SpecialType(typeof(Emote))]
            public string UpvoteEmote { get; set; } = "👍";

            [Config("Downvote emote for suggestions"), SpecialType(typeof(Emote))]
            public string DownvoteEmote { get; set; } = "👎";

            [Config("Whether to remove command calls after execution")]
            public bool RemoveCommandMessages { get; set; }
            
            [Config("Role to give new members when they join"), List(typeof(SocketRole))]
            [BsonRepresentation(BsonType.String)]
            public ulong[] NewMemberRoles { get; private set; } = {};

            public class AnnounceSubModule : SubModule
            {
                [Config("Whether to directly send welcome messages to new users")]
                public bool DMNewUsers { get; set; } = false;

                [Config("Send welcome messages when a user has joined")]
                public bool Welcomes { get; set; } = true;

                [Config("Send goodbye messages when a user has left")]
                public bool Goodbyes { get; set; } = true;

                [Config("Welcome messages for new users")]
                //[ExtraInfo("**Placeholders:** `[GUILD]` or `[SERVER]` - Discord server name\n `[USER]` - Mention target server user\n")]
                public string[] WelcomeMessages { get; set; } = { "Welcome to [SERVER] [USER]!", "Welcome [USER] to [SERVER]!", "Hey [USER]! Welcome to [SERVER]!" };

                [Config("Goodbye messages for users")]
                public string[] GoodbyeMessages { get; set; } = { "[USER] left the server.", "It's sad to see you go... [USER].", "Bye [USER]!" };

                [Config("Channel for server welcome announcements"), SpecialType(typeof(SocketTextChannel))]
                [BsonRepresentation(BsonType.String)]
                public ulong Channel { get; set; }
            }
        }

        public class ModerationModule : CommandConfigModule
        {
            [Config("Allow 3PG to punish offenders!")]
            public AutoModerationSubModule Auto { get; private set; } = new AutoModerationSubModule();

            [Config("Allow logging of users' actions")]
            public StaffLogsSubModule StaffLogs { get; private set; } = new StaffLogsSubModule();

            [Config("Role automatically given to mute users")]
            public string MutedRoleName { get; private set; } = "Muted";

            [Config("Reset all user data on this server if they get banned")]
            public bool ResetBannedUsers { get; private set; }

            public class AutoModerationSubModule : SubModule
            {
                [Config("Use a list of predefined explicit words for auto detection")]
                public bool UseDefaultBanWords { get; set; } = true;

                [Config("Maximum amount of messages that can be sent in a minute by a user")]
                public int SpamThreshold { get; set; } = 10;

                [Config("Use a list of predefined explicit links for auto detection")]
                public bool UseDefaultBanLinks { get; set; } = false;

                [Config("Use your own or additional ban words")]
                public string[] CustomBanWords { get; set; } = {};

                [Config("Use your own or additional ban links")]
                public string[] CustomBanLinks { get; set; } = {};

                [Config("Warnings required to auto-kick the offender"), Range(-1, 50)]
                public int WarningsForKick { get; set; } = 5;

                [Config("Warnings required to auto-ban the offender"), Range(-1, 50)]
                public int WarningsForBan { get; set; } = 10;

                [Config("Length of time to auto-mute the offender")]
                public int AutoMuteSeconds { get; set; } = -1;

                [Config("Prevent user access if they have an explicit username/nickname")]
                public bool NicknameFilter { get; set; } = true;

                [Config("Punishment to users who have an explicit username"), Dropdown(typeof(PunishmentType))]
                public PunishmentType ExplicitUsernamePunishment { get; set; } = PunishmentType.Kick;

                [Config("Inform users that are spamming chat")]
                public bool SpamNotification { get; set; } = true;
            }

            public class StaffLogsSubModule : SubModule
            {
                [Config("Channel for logs")]
                [BsonRepresentation(BsonType.String), SpecialType(typeof(SocketTextChannel))]
                public ulong Channel { get; set; }

                [Config("The events to log"), List(typeof(LogEvent))]
                [BsonRepresentation(BsonType.String)]
                public LogEvent[] LogEvents { get; set; } = { LogEvent.Ban, LogEvent.Unban, LogEvent.Mute, LogEvent.Unmute, LogEvent.Kick, LogEvent.Warn, LogEvent.MessageDeleted };
            }
        }

        public class MusicModule : CommandConfigModule
        {
            [Config("Default volume for music, set when 3PG first plays tracks"), Range(0, 200)]
            public int DefaultVolume { get; private set; } = 100;

            // [Config("Whether the bot joins the channel on play")]
            // public bool JoinOnPlay { get; private set; } = true;

            [Config("The maximum allowed duration in hours for a track"), Range(0.25f, 24)]
            public float MaxTrackHours { get; private set; } = 2;

            [Config("Whether users have to vote for tracks to be skipped")]
            public bool VoteToSkip { get; private set; }

            [Config("Whether all voice channel members have to vote to skip, otherwise at least 50% of members are required")]
            public bool AllVotesToSkip { get; private set; }
        }

        public class XPModule : CommandConfigModule
        {
            public enum MessageType { AnyChannel, DM, SpecificChannel }

            [Config("Reward roles as XP rewards")]
            public RoleRewardsSubModule RoleRewards { get; private set; } = new RoleRewardsSubModule();
            
            [Config("Let users know when they level up")]
            public MessagesSubmodule Messages { get; private set; } = new MessagesSubmodule();

            [Config("The amount of EXP each message receives")]
            public int EXPPerMessage { get; set; } = 50;

            [Config("Minimum character length for a message to earn EXP"), Range(1, 100)]
            public int MessageLengthThreshold { get; set; } = 3;

            [Config("How long the user has to wait to earn EXP again")]
            public int Cooldown { get; set; } = 5;
            
            [Config("A cooldown given to users after being muted")]
            public int ExtendedCooldown { get; set; } = 300;

            [Config("Delay when to allow messages with identical content to the last")]
            public int DuplicateMessageThreshold { get; set; } = 5;

            [Config("Text channels where EXP cannot be earned"), List(typeof(SocketTextChannel))]
            [BsonRepresentation(BsonType.String)]
            public ulong[] ExemptChannels { get; set; } = {};
            
            [Config("Having any of these roles stops a user from earning EXP"), List(typeof(SocketRole))]
            [BsonRepresentation(BsonType.String)]
            public ulong[] ExemptRoles { get; set; } = {};

            [Config("Leaderboard command maximum page"), Range(1, 1000)]
            public int MaxLeaderboardPage { get; set; } = 100;

            public class RoleRewardsSubModule : SubModule
            {
                public SocketRole this[int levelNumber]
                {
                    get
                    {
                        LevelRoles.TryGetValue(levelNumber.ToString(), out ulong id);
                        return DiscordGuild.GetRole(id);
                    }
                    set => LevelRoles[$"{levelNumber}"] = value.Id;
                }

                public bool RolesExist => LevelRoles.Count > 0;

                [Config("Whether old XP roles should be removed after one is added")]
                public bool StackRoles { get; set; } = true;

                [Config("Required levels to reward roles")]
                [BsonRepresentation(BsonType.String)] 
                public Dictionary<string, ulong> LevelRoles { get; set; } = new Dictionary<string, ulong> {};
            }

            public class MessagesSubmodule : SubModule
            {
                [Config("Method for sending XP messages"), Dropdown(typeof(MessageMethod))]
                public MessageMethod Method { get; set; } = MessageMethod.DM;

                [Config("Specific channel for sending messages"), SpecialType(typeof(SocketTextChannel))]
                [BsonRepresentation(BsonType.String)]
                public ulong XPChannel { get; set; }
            }
        }

        public class SettingsModule : ConfigModule
        {
            [Config("Minimum permissions for using members using webapp features")]
            public PermissionsSubModule Permissions { get; private set; } = new PermissionsSubModule();

            public class PermissionsSubModule : SubModule
            {
                [Config("Minimum permission for editing server modules"), Dropdown(typeof(GuildPermission))]
                public GuildPermission EditModules { get; set; } = GuildPermission.ManageGuild;

                [Config("Required permission for viewing punishments"), Dropdown(typeof(GuildPermission))]
                public GuildPermission ViewPunishments { get; set; } = GuildPermission.ViewAuditLog;

                [Config("Whether anyone can view this server's leaderboard, or only server members can view it")]
                public bool IsLeaderboardPublic { get; set; } = true;

                [Config("Whether this server appears on the global leaderboard")]
                public bool AppearOnGlobalLeaderboard { get; set; } = false;
            }
        }
    }
}