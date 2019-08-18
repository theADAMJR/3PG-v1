using Bot3PG.DataStructs;
using Discord.WebSocket;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bot3PG.Core.Data
{
    public static class Users
    {
        private static readonly IMongoCollection<User> collection;
        private const string userCollection = "user";

        private static readonly DatabaseManager db;

        static Users()
        {
            db = new Lazy<DatabaseManager>().Value;
            collection = db.Database.GetCollection<User>(userCollection);
            if (collection is null)
            {
                db.Database.CreateCollection("user");
                collection = db.Database.GetCollection<User>(userCollection);
            }
        }

        public static async void OnUserUpdated(GuildUser guildUser)
        {
            Console.WriteLine($"{guildUser.ID} updated!");
            var user = await db.GetAsync(guildUser.ID, collection);
            user[guildUser.ID] = guildUser;
            await db.UpdateAsync(guildUser.ID, user, collection);
        }

        public static async Task<User> GetAsync(SocketUser socketUser) => await GetOrCreateAsync(socketUser);
        public static async Task<GuildUser> GetAsync(SocketGuildUser socketGuildUser) => await GetOrCreateAsync(socketGuildUser);

        private static async Task<User> GetOrCreateAsync(SocketUser socketUser)
        {
            Console.WriteLine("get " + socketUser.Username);
            var user = await db.GetAsync(socketUser.Id, collection);
            Console.WriteLine("2");

            if (user is null)
            {
                Console.WriteLine("user is null");
                return await CreateUserAsync(socketUser);
            }
            return user;
        }

        private static async Task<GuildUser> GetOrCreateAsync(SocketGuildUser socketGuildUser)
        {
            var user = await GetOrCreateAsync(socketGuildUser as SocketUser);
            var guildUser = user?[socketGuildUser.Guild.Id];

            return (guildUser is null) ? await CreateGuildUserAsync(socketGuildUser) : guildUser;
        }

        private static async Task<User> CreateUserAsync(SocketUser socketUser)
        {
            if (socketUser.IsBot) return null;

            var newUser = new User(socketUser);

            await db.InsertAsync(newUser, collection);
            return newUser;
        }

        private static async Task<GuildUser> CreateGuildUserAsync(SocketGuildUser socketGuildUser)
        {
            if (socketGuildUser.IsBot) return null;

            var user = await db.GetAsync(socketGuildUser.Id, collection);
            var guildUser = new GuildUser(socketGuildUser);
            user[socketGuildUser.Guild.Id] = guildUser;
            await db.UpdateAsync(socketGuildUser.Id, user, collection);

            return user[socketGuildUser.Guild.Id];
        }

        public static List<GuildUser> GetGuildUsers(SocketGuild socketGuild)
        {
            var documents = collection.Find(Builders<User>.Filter.Empty).ToList();
            var guildUsers = new List<GuildUser>();

            foreach (var user in documents)
            {
                var matchingGuild = user[socketGuild.Id].ToJson();
                if (matchingGuild is null) continue;

                guildUsers.Add(JsonConvert.DeserializeObject(matchingGuild) as GuildUser);
            }
            return guildUsers;
        }

        public static async void ResetAsync(SocketGuildUser socketGuildUser)
        {
            DeleteGuildUser(socketGuildUser);
            await CreateGuildUserAsync(socketGuildUser);
        }

        public static void DeleteGuildUser(SocketGuildUser socketGuildUser)
        {
            var filter = Builders<User>.Filter.Eq("_id", socketGuildUser.Id);
            var userDocument = collection.Find(filter).First().ToJson();

            collection.DeleteOne(userDocument);
        }

        public static List<GuildUser> GetLeaderboardUsers(SocketGuild socketGuild)
        {
            return GetGuildUsers(socketGuild);

            /*var leaderboardUsers = new List<GuildUser>();
            foreach (var user in guildUsers)
            {
                var socketGuildUser = socketGuild.GetUser((ulong)user["user_id"]);
                var userAccount = Get(socketGuildUser);
                leaderboardUsers.Add(userAccount);
            }
            return leaderboardUsers;*/
        }
    }
}