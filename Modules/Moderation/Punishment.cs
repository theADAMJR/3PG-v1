using Discord.WebSocket;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Bot3PG.Modules.Moderation
{
    public class Punishment
    {
        public PunishmentType Type { get; private set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime Start { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime End { get; set; }

        public string Reason { get; set; }

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