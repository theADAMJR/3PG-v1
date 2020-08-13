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
        private static readonly IMongoCollection<GuildUser> guildUserCollection;

        private const string users = "user";
        private const string guildUsers = "guildUser";

        private static readonly DatabaseManager db;

        static Users()
        {
            db = new DatabaseManager(Global.Config.DB);

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

        public static async Task<User> GetAsync(IUser socketUser) => await GetOrCreateAsync(socketUser);
        public static async Task<GuildUser> GetAsync(IGuildUser socketGuildUser) => await GetOrCreateAsync(socketGuildUser);

        private static async Task<User> GetOrCreateAsync(IUser socketUser) 
            => socketUser is null ? null : await db.GetAsync(u => u.ID == socketUser.Id, userCollection) ?? await CreateUserAsync(socketUser);

        private static async Task<GuildUser> GetOrCreateAsync(IGuildUser socketGuildUser)
        {
            if (socketGuildUser is null) return null;

            await GetOrCreateAsync(socketGuildUser as SocketUser);
            var guildUser = await db.GetAsync(u => u.ID == socketGuildUser.Id && u.GuildID == socketGuildUser.Guild.Id, guildUserCollection);
            guildUser?.Reinitialize(socketGuildUser);

            return guildUser ?? await CreateGuildUserAsync(socketGuildUser);
        }

        private static async Task<User> CreateUserAsync(IUser socketUser)
        {
            var user = new User(socketUser);
            await db.InsertAsync(user, userCollection);
            return user;
        }
        private static async Task<GuildUser> CreateGuildUserAsync(IGuildUser socketGuildUser)
        {
            var guildUser = new GuildUser(socketGuildUser);
            await db.InsertAsync(guildUser, guildUserCollection);
            return guildUser;
        }

        public static async Task<List<GuildUser>> GetGuildUsersAsync(SocketGuild socketGuild) 
            => socketGuild is null ? null : await db.GetManyAsync(u => u.GuildID == socketGuild.Id, guildUserCollection);

        public async static Task<List<SocketGuildUser>> GetRankedGuildUsersAsync(SocketGuild socketGuild)
        {
            if (socketGuild is null)
                throw new ArgumentNullException(nameof(socketGuild));

            var guildUsers = await db.GetManyAsync(u => u.GuildID == socketGuild.Id, guildUserCollection);
            return guildUsers.OrderByDescending(u => u.XP.EXP).Select(u => socketGuild.GetUser(u.ID)).Where(u => u != null && !u.IsBot).ToList();
        }

        public static async Task ResetAsync(SocketGuildUser socketGuildUser)
        {
            await DeleteGuildUser(socketGuildUser);
            await CreateGuildUserAsync(socketGuildUser);
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

        public static async Task DeleteGuildUser(SocketUser socketUser) => await db.DeleteAsync(u => u.ID == socketUser.Id, userCollection);
        public static async Task DeleteGuildUser(SocketGuildUser socketGuildUser) => await db.DeleteAsync(u => u.ID == socketGuildUser.Id && u.GuildID == socketGuildUser.Guild.Id, guildUserCollection);
    }
}