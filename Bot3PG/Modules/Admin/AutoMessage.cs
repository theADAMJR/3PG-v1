using System;
using Bot3PG.Data.Structs;
using Discord.WebSocket;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Bot3PG.Modules.Admin
{
    public class AutoMessage
    {
        [Config("Message to send after each interval")]
        public string Message { get; set; }

        [Config("Channel to send message to"), SpecialType(typeof(SocketTextChannel))]
        [BsonRepresentation(BsonType.String)]
        public ulong Channel { get; set; }

        [Config("Time in hours between each message"), Range(0.05f, 168f)]
        public float Interval { get; set; } = 2f;
    }
}