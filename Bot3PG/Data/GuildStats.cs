using Discord.WebSocket;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Bot3PG.Data
{
    public class GuildStats
    {
        [BsonRepresentation(BsonType.String)]
        public ulong ID { get; }

        public CommandStat[] Commands { get; set; }

        public GuildStats(SocketGuild socketGuild) => ID = socketGuild.Id;
    }
}