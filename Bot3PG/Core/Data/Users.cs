using Bot3PG.DataStructs;
using Bot3PG.Services;
using Discord;
using Discord.WebSocket;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bot3PG.Core.Data
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

            userCollection = db.Database.GetCollection<User>(users);
            if (userCollection is null)
            {
                db.Database.CreateCollection(users);
                userCollection = db.Database.GetCollection<User>(users);
            }
            guildUserCollection = db.Database.GetCollection<GuildUser>(guildUsers);
            if (guildUserCollection is null)
            {
                db.Database.CreateCollection(guildUsers);
                guildUserCollection = db.Database.GetCollection<GuildUser>(guildUsers);
            }
        }

        public static async Task Save(User user) => await db.UpdateAsync(user.ID, user, userCollection);
        public static async Task Save(GuildUser guildUser) => await db.UpdateAsync(guildUser.GuildID, guildUser, guildUserCollection);

        public static async Task<User> GetAsync(SocketUser socketUser) => await GetOrCreateAsync(socketUser);
        public static async Task<GuildUser> GetAsync(SocketGuildUser socketGuildUser) => await GetOrCreateAsync(socketGuildUser);

        private static async Task<User> GetOrCreateAsync(SocketUser socketUser)
        {
            var user = await db.GetAsync(socketUser.Id, userCollection);
            return user ?? await CreateUserAsync(socketUser);
        }

        private static async Task<GuildUser> GetOrCreateAsync(SocketGuildUser socketGuildUser)
        {
            await GetOrCreateAsync(socketGuildUser as SocketUser);

            var guildUser = await db.GetAsync(socketGuildUser.Guild.Id, guildUserCollection);
            return guildUser ?? await CreateGuildUserAsync(socketGuildUser);
        }

        private static async Task<User> CreateUserAsync(SocketUser socketUser)
        {
            if (socketUser.IsBot) return null;

            var newUser = new User(socketUser);
            await db.InsertAsync(newUser, userCollection);
            return newUser;
        }

        private static async Task<GuildUser> CreateGuildUserAsync(SocketGuildUser socketGuildUser)
        {
            if (socketGuildUser.IsBot) return null;
            var guildUser = new GuildUser(socketGuildUser);
            await db.InsertAsync(guildUser, guildUserCollection);
            return guildUser;
        }

        public static async Task<List<GuildUser>> GetGuildUsersAsync(SocketGuild socketGuild)
        {
            var allGuildUsers = await db.GetAllAsync(guildUserCollection);
            var guildUsers = allGuildUsers.Where(u => u.GuildID == socketGuild.Id).ToList();
            return guildUsers;
        }

        public static async Task ResetAsync(SocketGuildUser socketGuildUser)
        {
            await DeleteGuildUser(socketGuildUser);
            await CreateGuildUserAsync(socketGuildUser);
        }

        public static async Task DeleteGuildUser(SocketUser socketGuildUser) => await db.DeleteAsync(socketGuildUser.Id, guildUserCollection);
        public static async Task DeleteGuildUser(SocketGuildUser socketGuildUser) => await db.DeleteAsync(socketGuildUser.Guild.Id, guildUserCollection);
    }
}