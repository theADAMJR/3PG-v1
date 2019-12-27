using Bot3PG.Handlers;
using Bot3PG.Modules.Moderation;
using Discord;
using Discord.WebSocket;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bot3PG.Data.Structs
{
    [BsonIgnoreExtraElements]
    public class GuildUser
    {
        public static Action<GuildUser, Punishment> Muted;
        public static Action<GuildUser, Punishment> Unmuted;
        public static Action<GuildUser, Punishment> Warned;

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

        public void Reinitialize(SocketGuildUser socketGuildUser) { _id = socketGuildUser.Id; _guildId = socketGuildUser.Guild.Id; }

        public GuildUser(SocketGuildUser socketGuildUser)
        {
            Reinitialize(socketGuildUser);
            ID = socketGuildUser.Id;
            GuildID = socketGuildUser.Guild.Id;
        }

        public async Task BanAsync(TimeSpan duration, string reason, SocketUser instigator)
        {
            var end = (duration.TotalDays >= TimeSpan.MaxValue.TotalDays) ? DateTime.MaxValue : DateTime.Now.Add(duration);
            Status.Punishments.Add(new Punishment(PunishmentType.Ban, reason, instigator, DateTime.Now, end));

            await DiscordUser.Guild.AddBanAsync(ID, options: new RequestOptions() { AuditLogReason = reason });
            if (DiscordUser.IsBot)
            {
                await DiscordUser.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed("Moderation", $"You have been banned from {DiscordUser.Guild.Name} for '{reason}'", Color.Red));
            }

            var guild = await GetGuild();
            if (guild.Moderation.ResetBannedUsers)
            {
                await Users.ResetAsync(DiscordUser);
            }
            await Users.Save(this);
        }

        public async Task MuteAsync(TimeSpan duration, string reason, SocketUser instigator)
        {
            var end = (duration.TotalDays == -1) ? DateTime.MaxValue : DateTime.Now.Add(duration);
            var punishment = new Punishment(PunishmentType.Mute, reason, instigator, DateTime.Now, end);
            Status.Punishments.Add(punishment);

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
            if (mutedRole != null) 
            {
                await DiscordUser.AddRoleAsync(mutedRole);
            }
            if (Muted != null) Muted(this, punishment);

            await Users.Save(this);
        }

        public async Task UnmuteAsync(string reason, SocketUser instigator)
        {
            Status.Mutes.LastOrDefault().End = DateTime.Now;

            var guild = await Guilds.GetAsync(DiscordUser.Guild);
            var mutedRole = DiscordUser.Guild.Roles.FirstOrDefault(r => r.Name == guild.Moderation.MutedRoleName);

            if (!DiscordUser.IsBot)
            {
                await DiscordUser.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed("Moderation", $"You have been unmuted from {DiscordUser.Guild.Name} for '{reason}'", Color.Green));
            }
            await DiscordUser.RemoveRoleAsync(mutedRole);
            if (Muted != null) Unmuted(this, null);

            await Users.Save(this);
        }

        public async Task KickAsync(string reason, SocketUser instigator)
        {
            Status.Punishments.Add(new Punishment(PunishmentType.Kick, reason, instigator, DateTime.Now, DateTime.Now));

            if (!DiscordUser.IsBot)
            {
                await DiscordUser.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed("Moderation", $"You have been kicked from {DiscordUser.Guild.Name} for '{reason}'", Color.Red));
            }
            await DiscordUser.KickAsync(reason, new RequestOptions() { AuditLogReason = reason });
            await Users.Save(this);
        }

        public async Task WarnAsync(string reason, SocketUser instigator, bool alertUser = true)
        {
            var punishment = new Punishment(PunishmentType.Warn, reason, instigator, DateTime.Now, DateTime.Now);
            Status.Punishments.Add(punishment);
            if (alertUser && !DiscordUser.IsBot)
            {
                await DiscordUser.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed("Moderation", $"You have been warned from {DiscordUser.Guild.Name} for '{reason}'", Color.Red));
            }
            if (Muted != null) Warned(this, punishment);

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
                if (user is null || guild is null) return false;

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
            public int MessageCount { get; set; }

            public List<Punishment> Punishments { get; internal set; } = new List<Punishment>();

            public List<Punishment> Bans => Punishments.Where(p => p.Type == PunishmentType.Ban)?.ToList();
            public List<Punishment> Mutes => Punishments.Where(p => p.Type == PunishmentType.Mute)?.ToList();
            public List<Punishment> Kicks => Punishments.Where(p => p.Type == PunishmentType.Kick)?.ToList();
            public List<Punishment> Warns => Punishments.Where(p => p.Type == PunishmentType.Warn)?.ToList();

            public int WarningsCount => Warns.Count;
        }
    }
}