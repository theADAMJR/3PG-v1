using Bot3PG.Core.Data;
using Bot3PG.Handlers;
using Bot3PG.Moderation;
using Discord;
using Discord.WebSocket;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bot3PG.DataStructs
{
    public partial class GuildUser
    {
        public delegate void GuildUserDelegate(GuildUser guildUser);
        public static event GuildUserDelegate GuildUserUpdated;

        [BsonIgnore]
        public ulong ID { get; set; }
        public ulong GuildID { get; set; }

        private SocketGuildUser socketGuildUser
        {
            get
            {
                var socketGuild = Global.Client.GetGuild(ID);
                return socketGuild.GetUser(ID);
            }
        }

        public Leveling XP = new Leveling();
        public Moderation Status = new Moderation();

        public GuildUser(SocketGuildUser socketGuildUser) 
        {
            ID = socketGuildUser?.Id ?? 0;
            GuildID = socketGuildUser?.Guild?.Id ?? 0;
        }

        private static GuildUser GetUser(SocketGuildUser socketGuildUser)
        {
            GuildUser user = null;
            new Task(async () =>
            {
                user = await Users.GetAsync(socketGuildUser);
            });
            return user;
        }

        public async Task BanAsync(TimeSpan duration, string reason)
        {
            Status[PunishmentType.Ban] = new Moderation.Punishment()
            {
                Start = DateTime.Now,
                End = (duration.TotalDays == -1) ? DateTime.MaxValue : DateTime.Now.Add(duration),
                Reason = reason
            };

            await socketGuildUser.Guild.AddBanAsync(ID);
            await socketGuildUser.SendMessageAsync("", embed: await EmbedHandler.CreateBasicEmbed("Moderation", $"You have been banned from {socketGuildUser.Guild.Name} for '{reason}'", Color.Red));
        }

        public async Task UnbanAsync(string reason)
        {
            Status[PunishmentType.Ban].End = DateTime.Now;

            await socketGuildUser.Guild.RemoveBanAsync(ID);
            await socketGuildUser.SendMessageAsync("", embed: await EmbedHandler.CreateBasicEmbed("Moderation", $"You have been unbanned from {socketGuildUser.Guild.Name} for '{reason}'", Color.Green));
        }

        public async Task MuteAsync(TimeSpan duration, string reason)
        {
            Status[PunishmentType.Mute] = new Moderation.Punishment()
            {
                Start = DateTime.Now,
                End = (duration.TotalDays == -1) ? DateTime.MaxValue : DateTime.Now.Add(duration),
                Reason = reason
            };

            await socketGuildUser.ModifyAsync(x => x.Mute = true);
            await socketGuildUser.SendMessageAsync("", embed: await EmbedHandler.CreateBasicEmbed("Moderation", $"You have been muted from {socketGuildUser.Guild.Name} for '{reason}'", Color.Red));
        }

        public async Task UnmuteAsync(string reason)
        {
            Status[PunishmentType.Mute].End = DateTime.Now;

            await socketGuildUser.ModifyAsync(x => x.Mute = false);
            await socketGuildUser.SendMessageAsync("", embed: await EmbedHandler.CreateBasicEmbed("Moderation", $"You have been unmuted from {socketGuildUser.Guild.Name} for '{reason}'", Color.Green));
        }

        public async Task KickAsync(string reason)
        {
            Status[PunishmentType.Kick] = new Moderation.Punishment()
            {
                Start = DateTime.Now,
                Reason = reason
            };

            await socketGuildUser.KickAsync(reason);
            await socketGuildUser.SendMessageAsync("", embed: await EmbedHandler.CreateBasicEmbed("Moderation", $"You have been kicked from {socketGuildUser.Guild.Name} for '{reason}'", Color.Red));
        }

        public async Task WarnAsync(string reason)
        {
            Status[PunishmentType.Warn] = new Moderation.Punishment()
            {
                Start = DateTime.Now,
                Reason = reason
            };

            await socketGuildUser.SendMessageAsync("", embed: await EmbedHandler.CreateBasicEmbed("Moderation", $"You have been warned from {socketGuildUser.Guild.Name} for '{reason}'", Color.Red));
        }

        public class Leveling
        {
            public DateTime LastXPMsg { get; set; }

            public int EXP { get; set; }

            public int EXPForNextLevel => (int)Math.Sqrt(Math.Pow(EXP, LevelNumber)) / 100;

            public uint LevelNumber => (uint)Math.Sqrt(EXP / 100) + 1;

            public bool InXPCooldown
            {
                get => true;
                /*get
                {
                    var lastMessageTime = DateTime.Now.Subtract(_user.XP.LastXPMsg);
                    return (lastMessageTime.TotalSeconds <= _guild.Config.XPCooldown || _user.Status.IsMuted) ? true : false;
                }*/
            }

            public void ExtendXPCooldown()
            {
                //var nextAvailableMsg = DateTime.Now.Add(TimeSpan.FromSeconds(_guild.Config.ExtendedXPCooldown * (1 + User.Status.WarningsCount)));
                //LastXPMsg = nextAvailableMsg;
            }
        }

        public partial class Moderation
        {

            private int lastPunishmentIndex(PunishmentType punishmentType) => Punishments[punishmentType].Count - 1;

            public Punishment this[PunishmentType punishmentType]
            {
                get => Punishments[punishmentType][lastPunishmentIndex(punishmentType)];
                set => Punishments[punishmentType][lastPunishmentIndex(punishmentType)] = value;
            }

            public bool IsMuted
            {
                get
                {
                    var punishment = Punishments[PunishmentType.Mute][lastPunishmentIndex(PunishmentType.Mute)];
                    return punishment.Start < punishment.End;
                }
            }
            public bool IsBanned
            {
                get
                {
                    var punishment = Punishments[PunishmentType.Mute][lastPunishmentIndex(PunishmentType.Ban)];
                    return punishment.Start < punishment.End;
                }
            }

            public bool AgreedToRules { get; set; }

            public Dictionary<PunishmentType, List<Punishment>> Punishments { get; internal set; } = new Dictionary<PunishmentType, List<Punishment>>();

            public int WarningsCount => Punishments[PunishmentType.Warn].Count;

            public class Punishment
            {
                public DateTime Start { get; set; }
                public DateTime End { get; set; }
                public string Reason { get; set; }
            }
        }
    }
}