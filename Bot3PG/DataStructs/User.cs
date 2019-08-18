using Bot3PG.Services;
using Discord.WebSocket;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bot3PG.DataStructs
{
    public class User
    {
        public GuildUser this[ulong guildId]
        {
            get
            {
                try
                {
                    string bsonGuildId = guildId.ToString();
                    return guilds[bsonGuildId];
                }
                catch (Exception error)
                {
                    new Task(async() => await LoggingService.LogCriticalAsync("database", error.Message)).Start();
                    foreach (var key in guilds.Keys)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Existing keys: " + key);
                    }
                    return null;
                }
            }
            set
            {
                string bsonGuildId = guildId.ToString();
                guilds[bsonGuildId] = value;
            }
        }

        [BsonId]
        public ulong ID { get; private set; }

        public string BannerURL { get; set; }

        [BsonRequired]
        private Dictionary<string, GuildUser> guilds = new Dictionary<string, GuildUser>();

        public User(SocketUser socketUser) => ID = socketUser?.Id ?? 0;
    }
}