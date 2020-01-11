using Bot3PG.Data.Structs;
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
        private static readonly IMongoCollection<GuildUser> guildUserCollection;

        private const string users = "user";
        private const string guildUsers = "guildUser";

        private static readonly DatabaseManager db;

        static Users()
        {
            db = new Lazy<DatabaseManager>().Value;

            var collections = db.Database.ListCollectionNames().ToList();
            
            if (!collections.Any(c => c == users))
                db.Database.CreateCollection(users);
            userCollection = db.Database.GetCollection<User>(users);

            if (!collections.Any(c => c == guildUsers))
                db.Database.CreateCollection(guildUsers);                
            guildUserCollection = db.Database.GetCollection<GuildUser>(guildUsers);
        }

        public static async Task Save(User user) => await db.UpdateAsync(u => u.ID == user.ID, user, userCollection);
        public static async Task Save(GuildUser guildUser) => await db.UpdateAsync(u => u.ID == guildUser.ID && u.GuildID == guildUser.GuildID, guildUser, guildUserCollection);

        public static async Task<User> GetAsync(SocketUser socketUser) => await GetOrCreateAsync(socketUser);
        public static async Task<GuildUser> GetAsync(SocketGuildUser socketGuildUser) => await GetOrCreateAsync(socketGuildUser);

        private static async Task<User> GetOrCreateAsync(SocketUser socketUser) 
            => socketUser is null ? null : await db.GetAsync(u => u.ID == socketUser.Id, userCollection) ?? await CreateUserAsync(socketUser);

        private static async Task<GuildUser> GetOrCreateAsync(SocketGuildUser socketGuildUser)
        {
            if (socketGuildUser is null) return null;

            await GetOrCreateAsync(socketGuildUser as SocketUser);
            var guildUser = await db.GetAsync(u => u.ID == socketGuildUser.Id && u.GuildID == socketGuildUser.Guild.Id, guildUserCollection);
            guildUser?.Reinitialize(socketGuildUser);

            return guildUser ?? await CreateGuildUserAsync(socketGuildUser);
        }

        private static async Task<User> CreateUserAsync(SocketUser socketUser)
        {
            var user = new User(socketUser);
            await db.InsertAsync(user, userCollection);
            return user;
        }
        private static async Task<GuildUser> CreateGuildUserAsync(SocketGuildUser socketGuildUser)
        {
            var guildUser = new GuildUser(socketGuildUser);
            await db.InsertAsync(guildUser, guildUserCollection);
            return guildUser;
        }

        public static async Task<List<GuildUser>> GetGuildUsersAsync(SocketGuild socketGuild) 
            => socketGuild is null ? null : await db.GetManyAsync(u => u.GuildID == socketGuild.Id, guildUserCollection);

        public static async Task ResetAsync(SocketGuildUser socketGuildUser)
        {
            await DeleteGuildUser(socketGuildUser);
            await CreateGuildUserAsync(socketGuildUser);
        }

        public static async Task DeleteGuildUser(SocketUser socketUser) => await db.DeleteAsync(u => u.ID == socketUser.Id, userCollection);
        public static async Task DeleteGuildUser(SocketGuildUser socketGuildUser) => await db.DeleteAsync(u => u.ID == socketGuildUser.Id && u.GuildID == socketGuildUser.Guild.Id, guildUserCollection);
    }
}