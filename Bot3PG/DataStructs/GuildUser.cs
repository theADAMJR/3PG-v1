using Bot3PG.Core.Data;
using Bot3PG.Handlers;
using Bot3PG.Moderation;
using Discord;
using Discord.WebSocket;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bot3PG.DataStructs
{
    public class GuildUser : GlobalEntity<ulong>
    {
        private static ulong _guildId;
        [BsonId] public ulong GuildID { get => _guildId; set => _guildId = value; }

        public static SocketGuildUser _SocketGuildUser => Global.Client.GetGuild(_guildId)?.GetUser(_ID);

        private async static Task<GuildUser> GetUser() => await Users.GetAsync(_SocketGuildUser);
        private async static Task<Guild> GetGuild() => await Guilds.GetAsync(_SocketGuildUser.Guild);

        public Leveling XP { get; set; } = new Leveling();
        public Moderation Status { get; set; } = new Moderation();

        public GuildUser(SocketGuildUser socketGuildUser)
        {
            ID = socketGuildUser.Id;
            GuildID = socketGuildUser.Guild.Id;
        }

        public async Task BanAsync(TimeSpan duration, string reason)
        {
            var end = (duration.TotalDays == -1) ? DateTime.MaxValue : DateTime.Now.Add(duration);
            Status.Punishments.Add(new Moderation.Punishment(PunishmentType.Ban, reason, DateTime.Now, end));

            await _SocketGuildUser.Guild.AddBanAsync(ID, options: new RequestOptions() { AuditLogReason = reason });
            await _SocketGuildUser.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed("Moderation", $"You have been banned from {_SocketGuildUser.Guild.Name} for '{reason}'", Color.Red));
            await Users.Save(this);
        }

        public async Task UnbanAsync(string reason)
        {
            Status[PunishmentType.Ban].End = DateTime.Now;

            await _SocketGuildUser.Guild.RemoveBanAsync(ID, new RequestOptions() { AuditLogReason = reason });
            await _SocketGuildUser.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed("Moderation", $"You have been unbanned from {_SocketGuildUser.Guild.Name} for '{reason}'", Color.Green));
            await Users.Save(this);
        }

        public async Task MuteAsync(TimeSpan duration, string reason)
        {
            var end = (duration.TotalDays == -1) ? DateTime.MaxValue : DateTime.Now.Add(duration);
            Status.Punishments.Add(new Moderation.Punishment(PunishmentType.Mute, reason, DateTime.Now, end));
            await _SocketGuildUser.ModifyAsync(x => x.Mute = true, new RequestOptions() { AuditLogReason = reason });
            await _SocketGuildUser.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed("Moderation", $"You have been muted from {_SocketGuildUser.Guild.Name} for '{reason}'", Color.Red));
            await Users.Save(this);
        }

        public async Task UnmuteAsync(string reason)
        {
            Status[PunishmentType.Mute].End = DateTime.Now;

            await _SocketGuildUser.ModifyAsync(x => x.Mute = false, new RequestOptions() { AuditLogReason = reason });
            await _SocketGuildUser.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed("Moderation", $"You have been unmuted from {_SocketGuildUser.Guild.Name} for '{reason}'", Color.Green));
            await Users.Save(this);
        }

        public async Task KickAsync(string reason)
        {
            Status.Punishments.Add(new Moderation.Punishment(PunishmentType.Kick, reason, DateTime.Now));
            await _SocketGuildUser.KickAsync(reason, new RequestOptions() { AuditLogReason = reason });
            await _SocketGuildUser.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed("Moderation", $"You have been kicked from {_SocketGuildUser.Guild.Name} for '{reason}'", Color.Red));
            await Users.Save(this);
        }

        public async Task WarnAsync(string reason)
        {
            Status.Punishments.Add(new Moderation.Punishment(PunishmentType.Warn, reason));
            await _SocketGuildUser.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed("Moderation", $"You have been warned from {_SocketGuildUser.Guild.Name} for '{reason}'", Color.Red));
            await Users.Save(this);
        }

        public class Leveling
        {
            [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
            public DateTime LastXPMsg { get; set; }
            public int EXP { get; set; }

            public int EXPForNextLevel => (int)((Math.Pow((int)LevelNumber + 1, 2)) * 100) - EXP;
            public uint LevelNumber => (uint)Math.Sqrt(EXP / 100) + 1;

            public async Task<bool> GetInXPCooldown()
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
            private int LastPunishmentIndex(PunishmentType punishmentType) => Punishments.Where(p => p.Type == punishmentType).Count() - 1;

            public Punishment this[PunishmentType punishmentType]
            {
                get => Punishments.Count > 0 ? Punishments[LastPunishmentIndex(punishmentType)] : null;
                set => Punishments[LastPunishmentIndex(punishmentType)] = value;
            }

            public bool IsMuted
            {
                get
                {
                    var punishment = this[PunishmentType.Mute];
                    return punishment != null && punishment.Start < punishment.End;
                }
            }
            public bool IsBanned
            {
                get
                {
                    var punishment = this[PunishmentType.Ban];
                    return punishment != null && punishment.Start < punishment.End;
                }
            }

            // TODO - last message

            public bool AgreedToRules { get; set; }

            public List<Punishment> Punishments { get; internal set; } = new List<Punishment>();

            public List<Punishment> Bans => Punishments.Where(p => p.Type == PunishmentType.Ban).ToList();
            public List<Punishment> Mutes => Punishments.Where(p => p.Type == PunishmentType.Mute).ToList();
            public List<Punishment> Kicks => Punishments.Where(p => p.Type == PunishmentType.Kick).ToList();
            public List<Punishment> Warns => Punishments.Where(p => p.Type == PunishmentType.Warn).ToList();

            public int WarningsCount => Warns.Count;

            public class Punishment
            {
                public PunishmentType Type { get; set; }
                public DateTime Start { get; set; }
                public DateTime End { get; set; }
                public string Reason { get; set; }

                public Punishment(PunishmentType type, string reason, DateTime start = default, DateTime end = default)
                {
                    Type = type;
                    Start = start;
                    End = end;
                    Reason = reason;
                }
            }
        }
    }
}