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
    public static class GuildUsers
    {
        private static readonly IMongoCollection<GuildUser> guildUserCollection;
        private static readonly DatabaseManager db = new DatabaseManager(Global.Config.MongoURI);

        private const string guildUsers = "guildUser";

        static GuildUsers()
        {
            var collections = db.Database.ListCollectionNames().ToList();

            if (!collections.Any(c => c == guildUsers))
                db.Database.CreateCollection(guildUsers);                
            guildUserCollection = db.Database.GetCollection<GuildUser>(guildUsers);
        }

        public static async Task Save(GuildUser guildUser)
            => await db.UpdateAsync(u => u.ID == guildUser.ID && u.GuildID == guildUser.GuildID, guildUser, guildUserCollection);

        public static async Task<GuildUser> GetAsync(IGuildUser socketGuildUser) => await GetOrCreateAsync(socketGuildUser);

        private static async Task<GuildUser> GetOrCreateAsync(IGuildUser socketGuildUser)
        {
            if (socketGuildUser is null) return null;

            var guildUser = await db.GetAsync(u =>
                u.ID == socketGuildUser.Id
                && u.GuildID == socketGuildUser.Guild.Id, guildUserCollection);
            guildUser?.Reinitialize(socketGuildUser);

            return guildUser ?? await CreateGuildUserAsync(socketGuildUser);
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
            return guildUsers
                .OrderByDescending(u => u.XP.EXP)
                .Select(u => socketGuild.GetUser(u.ID))
                .Where(u => u != null && !u.IsBot)
                .ToList();
        }

        public static async Task ResetAsync(SocketGuildUser socketGuildUser)
        {
            await DeleteGuildUser(socketGuildUser);
            await CreateGuildUserAsync(socketGuildUser);
        }

        public static async Task DeleteGuildUser(SocketGuildUser socketGuildUser)
            => await db.DeleteAsync(u => u.ID == socketGuildUser.Id && u.GuildID == socketGuildUser.Guild.Id, guildUserCollection);
    }
}