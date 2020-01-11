using Discord.WebSocket;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace Bot3PG.Data.Structs
{
    public class User
    {
        [BsonIgnore] private static ulong _id;
        [BsonRepresentation(BsonType.String)]
        [BsonId] public ulong ID { get; private set; }

        public string BannerURL { get; set; } = "";
        public int MessageCount { get; set; }

        public User(SocketUser socketUser) { _id = socketUser.Id; ID = socketUser.Id; }
    }
}