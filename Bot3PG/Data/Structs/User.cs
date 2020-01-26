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

        public int MessageCount { get; set; }
        public int Reputation { get; set; }
        public bool IsPrivate { get; set; }

        public XPCardSettings XPCard { get; set; } = new XPCardSettings();

        public class XPCardSettings
        {
            public string BackgroundURL { get; set; }
            public string UsernameColour { get; set; }
            public string EXPColour { get; set; }
            public string RankColour { get; set; }
        }

        public User(SocketUser socketUser) { _id = socketUser.Id; ID = socketUser.Id; }
        public void Reinitialize() => XPCard ??= new XPCardSettings();
    }
}