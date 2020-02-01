using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Bot3PG.Data
{
    public class CommandStat
    {
        public string Name { get; set; } = "";

        [BsonRepresentation(BsonType.String)]
        public ulong InstigatorID { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}