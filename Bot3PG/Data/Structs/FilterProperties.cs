using Bot3PG.Modules.Moderation;
using Discord.WebSocket;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Bot3PG.Data.Structs
{
    public class FilterProperties
    {
        [Config("The message filter type"), Dropdown(typeof(FilterType))]
        public FilterType Filter { get; set; }

        [BsonRepresentation(BsonType.String), Config("Roles that are not affected by the filter"), List(typeof(SocketRole))]
        public ulong[] ExemptRoles { get; set; } = {};

        [BsonRepresentation(BsonType.String), Config("Text channels that are not affected by the filter"), List(typeof(SocketTextChannel))]
        public ulong[] ExemptChannels { get; set; } = {};

        [Config("The punishment when the message is filtered"), Dropdown(typeof(PunishmentType))]
        public PunishmentType Punishment { get; set; }
    }
}