using Discord.WebSocket;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace Bot3PG.DataStructs
{
#nullable enable
    public class User : GlobalEntity<ulong>
    {
        [BsonId] public new ulong ID { get => _ID; internal set => _ID = value; }

        public string BannerURL { get; set; } = "";

        public User(SocketUser socketUser) => ID = socketUser.Id;
    }
}