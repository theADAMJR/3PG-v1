using System;
using System.Collections.Generic;
using Discord.WebSocket;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Bot3PG.Data
{
    public class GuildStats
    {
        [BsonId, BsonRepresentation(BsonType.String)]
        public ulong ID { get; private set; }
        
        public List<CommandStat> Commands { get; set; } = new List<CommandStat>{};

        public GuildStats(SocketGuild socketGuild) => ID = socketGuild.Id;

        public void Reinitialize(SocketGuild socketGuild) => ID = socketGuild.Id;
    }
}