using Bot3PG.Data.Structs;
using Discord;
using Discord.WebSocket;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bot3PG.Data
{
    public static class Users
    {
        private static readonly IMongoCollection<User> userCollection;
        private static readonly DatabaseManager db = new DatabaseManager(Global.Config.MongoURI);

        private const string users = "user";

        static Users()
        {
            var collections = db.Database.ListCollectionNames().ToList();
            
            if (!collections.Any(c => c == users))
                db.Database.CreateCollection(users);
            userCollection = db.Database.GetCollection<User>(users);
        }

        public static async Task Save(User user)
            => await db.UpdateAsync(u => u.ID == user.ID, user, userCollection);

        public static async Task<User> GetAsync(IUser socketUser) => await GetOrCreateAsync(socketUser);

        private static async Task<User> GetOrCreateAsync(IUser socketUser) 
            => socketUser is null ? null : await db.GetAsync(u => u.ID == socketUser.Id, userCollection)
                ?? await CreateUserAsync(socketUser);

        private static async Task<User> CreateUserAsync(IUser socketUser)
        {
            var user = new User(socketUser);
            await db.InsertAsync(user, userCollection);
            return user;
        }

        public static async Task CheckReputationAdded(SocketUserMessage message, SocketReaction reaction)
        {
            var guildAuthor = message.Author as SocketGuildUser;
            if (guildAuthor is null || reaction.Emote.Name != "👍" || guildAuthor.Id == reaction.UserId) return;

            var user = await GetAsync(guildAuthor as SocketUser);
            user.Reputation++;
            await Save(user);
        }

        public static async Task CheckReputationRemoved(SocketUserMessage message, SocketReaction reaction)
        {
            var guildAuthor = message.Author as SocketGuildUser;
            if (guildAuthor is null || reaction.Emote.Name != "👍" || guildAuthor.Id == reaction.UserId) return;

            var user = await GetAsync(guildAuthor as SocketUser);
            user.Reputation--;
            await Save(user);
        }

        public static async Task<IEnumerable<User>> PurgeUsers()
        {
            var users = await db.GetManyAsync(u => u.MessageCount == 0, userCollection);
            foreach (var user in users)
                await db.DeleteAsync(u => u.ID == user.ID, userCollection);
            return users;
        }

        public static async Task DeleteGuildUser(SocketUser socketUser)
            => await db.DeleteAsync(u => u.ID == socketUser.Id, userCollection);
    }
}