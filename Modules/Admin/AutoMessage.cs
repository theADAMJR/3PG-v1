using System;
using Bot3PG.Data.Structs;
using Discord.WebSocket;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Bot3PG.Modules.Admin
{
    public class AutoMessage
    {
        [Config("Message to send after each interval. Multiple messages are sent at random.")]
        public string[] Message { get; set; } = {};

        [Config("Channel to send message to"), SpecialType(typeof(SocketTextChannel))]
        [BsonRepresentation(BsonType.String)]
        public ulong Channel { get; set; }

        [Config("Time in minutes between each message"), MinMax(5, 10080)]
        public int Interval { get; set; }
    }
}