using Bot3PG.Core.Data;
using Bot3PG.DataStructs.Attributes;
using Bot3PG.Handlers;
using Bot3PG.Moderation;
using Discord;
using Discord.WebSocket;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bot3PG.DataStructs
{
    [BsonIgnoreExtraElements]
    public class GuildUser
    {
        private static ulong _id;
        [BsonRepresentation(BsonType.String)]
        [BsonRequired] public ulong ID { get; private set; }

        private static ulong _guildId;
        [BsonRepresentation(BsonType.String)]
        [BsonRequired] public ulong GuildID { get => _guildId; private set => _guildId = value; }

        public static SocketGuildUser DiscordUser => Global.Client.GetGuild(_guildId)?.GetUser(_id);

        private async static Task<GuildUser> GetUser() => await Users.GetAsync(DiscordUser);
        private async static Task<Guild> GetGuild() => await Guilds.GetAsync(DiscordUser?.Guild);

        public Leveling XP { get; private set; } = new Leveling();
        public Moderation Status { get; private set; } = new Moderation();

        public void Reinitialize(SocketGuildUser socketGuildUser)
        {
            _id = socketGuildUser.Id;
            _guildId = socketGuildUser.Guild.Id;
        }

        public GuildUser(SocketGuildUser socketGuildUser)
        {
            Reinitialize(socketGuildUser);
            ID = socketGuildUser.Id;
            GuildID = socketGuildUser.Guild.Id;
        }

        public async Task BanAsync(TimeSpan duration, string reason, SocketUser instigator)
        {
            var end = (duration.TotalDays == -1) ? DateTime.MaxValue : DateTime.Now.Add(duration);
            Status.Punishments.Add(new Moderation.Punishment(PunishmentType.Ban, reason, instigator, DateTime.Now, end));

            await DiscordUser.Guild.AddBanAsync(ID, options: new RequestOptions() { AuditLogReason = reason });
            if (DiscordUser.IsBot)
            {
                await DiscordUser.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed("Moderation", $"You have been banned from {DiscordUser.Guild.Name} for '{reason}'", Color.Red));
            }
            await Users.Save(this);
        }

        public async Task UnbanAsync(string reason, SocketUser instigator)
        {
            Status.Bans.LastOrDefault().End = DateTime.Now;
            Status.Punishments.Add(new Moderation.Punishment(PunishmentType.Unban, reason, instigator, DateTime.Now));

            await DiscordUser.Guild.RemoveBanAsync(ID, new RequestOptions() { AuditLogReason = reason });
            if (!DiscordUser.IsBot)
            {
                await DiscordUser.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed("Moderation", $"You have been unbanned from {DiscordUser.Guild.Name} for '{reason}'", Color.Green));
            }
            await Users.Save(this);
        }

        public async Task MuteAsync(TimeSpan duration, string reason, SocketUser instigator)
        {
            var end = (duration.TotalDays == -1) ? DateTime.MaxValue : DateTime.Now.Add(duration);
            Status.Punishments.Add(new Moderation.Punishment(PunishmentType.Mute, reason, instigator, DateTime.Now, end));

            var socketGuild = DiscordUser.Guild;
            var guild = await Guilds.GetAsync(socketGuild);

            var mutedRole = socketGuild.Roles.FirstOrDefault(r => r.Name == guild.Moderation.MutedRoleName);
            if (mutedRole is null)
            {
                await socketGuild.CreateRoleAsync(guild.Moderation.MutedRoleName, GuildPermissions.None);
            }
            if (!DiscordUser.IsBot)
            {
                await DiscordUser.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed("Moderation", $"You have been muted from {DiscordUser.Guild.Name} for '{reason}'", Color.Red));
            }
            await Users.Save(this);
        }

        public async Task UnmuteAsync(string reason, SocketUser instigator)
        {
            Status.Mutes.LastOrDefault().End = DateTime.Now;

            var guild = await Guilds.GetAsync(DiscordUser.Guild);
            var mutedRole = DiscordUser.Guild.Roles.FirstOrDefault(r => r.Name == guild.Moderation.MutedRoleName);

            await DiscordUser.RemoveRoleAsync(mutedRole);
            if (!DiscordUser.IsBot)
            {
                await DiscordUser.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed("Moderation", $"You have been unmuted from {DiscordUser.Guild.Name} for '{reason}'", Color.Green));
            }
            await Users.Save(this);
        }

        public async Task KickAsync(string reason, SocketUser instigator)
        {
            Status.Punishments.Add(new Moderation.Punishment(PunishmentType.Kick, reason, instigator, DateTime.Now, DateTime.Now));
            await DiscordUser.KickAsync(reason, new RequestOptions() { AuditLogReason = reason });

            if (!DiscordUser.IsBot)
            {
                await DiscordUser.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed("Moderation", $"You have been kicked from {DiscordUser.Guild.Name} for '{reason}'", Color.Red));
            }
            await Users.Save(this);
        }

        public async Task WarnAsync(string reason, SocketUser instigator, bool alertUser = true)
        {
            Status.Punishments.Add(new Moderation.Punishment(PunishmentType.Warn, reason, instigator, DateTime.Now, DateTime.Now));
            if (alertUser && !DiscordUser.IsBot)
            {
                await DiscordUser.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed("Moderation", $"You have been warned from {DiscordUser.Guild.Name} for '{reason}'", Color.Red));
            }
            await Users.Save(this);
        }

        public class Leveling
        {
            [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
            public DateTime LastXPMsg { get; set; }
            public int EXP { get; set; }

            public int EXPForNextLevel => (int)((75 * Math.Pow(Level + 1, 2)) + (75 * (Level + 1)) - 150) - EXP;
            public int Level => (int)(-75 + Math.Sqrt(Math.Pow(75, 2) - 300 * (-150 - EXP))) / 150;

            public async Task<bool> GetXPCooldown()
            {
                var user = await GetUser();
                var guild = await GetGuild();

                var lastMessageTime = DateTime.Now.Subtract(user.XP.LastXPMsg);
                return lastMessageTime.TotalSeconds <= guild.XP.Cooldown || user.Status.IsMuted;
            }

            public async Task ExtendXPCooldown()
            {
                var user = await GetUser();
                var guild = await GetGuild();

                var nextAvailableMsg = DateTime.Now.Add(TimeSpan.FromSeconds(guild.XP.ExtendedCooldown * (1 + user.Status.WarningsCount)));
                LastXPMsg = nextAvailableMsg;
            }
        }

        public class Moderation
        {
            public bool IsMuted => Mutes.LastOrDefault() != null && DateTime.Now < Mutes.LastOrDefault().End;
            public bool IsBanned => Bans.LastOrDefault() != null && DateTime.Now < Bans.LastOrDefault().End;

            public string LastMessage { get; set; }

            public bool AgreedToRules { get; set; }

            public List<Punishment> Punishments { get; internal set; } = new List<Punishment>();

            public List<Punishment> Bans => Punishments.Where(p => p.Type == PunishmentType.Ban)?.ToList();
            public List<Punishment> Mutes => Punishments.Where(p => p.Type == PunishmentType.Mute)?.ToList();
            public List<Punishment> Kicks => Punishments.Where(p => p.Type == PunishmentType.Kick)?.ToList();
            public List<Punishment> Warns => Punishments.Where(p => p.Type == PunishmentType.Warn)?.ToList();

            public int WarningsCount => Warns.Count;

            public class Punishment
            {
                public PunishmentType Type { get; private set; }

                [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
                public DateTime Start { get; set; }

                [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
                public DateTime End { get; set; }

                public string Reason { get; private set; }

                public ulong InstigatorID { get; private set; }

                public Punishment(PunishmentType type, string reason, SocketUser instigator, DateTime start = default, DateTime end = default)
                {
                    Type = type;
                    Start = start;
                    End = end;
                    Reason = reason;
                    InstigatorID = instigator.Id;
                }
            }
        }
    }
}