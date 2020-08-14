using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using MongoDB.Driver;

namespace Bot3PG.Data
{
    public static class Stats
    { 
        private static readonly IMongoCollection<GuildStats> statsCollection;
        private static readonly DatabaseManager db = new DatabaseManager(Global.Config.MongoURI);

        private const string stats = "stats";
        
        static Stats()
        {
            var collections = db.Database.ListCollectionNames().ToList();
            
            if (!collections.Any(c => c == stats))
                db.Database.CreateCollection(stats);
            statsCollection = db.Database.GetCollection<GuildStats>(stats);
        }

        public static async Task LogCommandAsync(string name, SocketGuildUser instigator)
        {
            var stats = await Get(instigator.Guild);
            stats.Reinitialize(instigator.Guild);
            stats.Commands.Add(new CommandStat{ Name = name, InstigatorID = instigator.Id });
            await Save(stats);
        }

        public static async Task<GuildStats> Save(GuildStats stats) => await db.UpdateAsync(s => s.ID == stats.ID, stats, statsCollection);

        public static async Task<GuildStats> Get(SocketGuild guild) => await GetOrCreate(guild);

        private static async Task<GuildStats> GetOrCreate(SocketGuild guild) => await db.GetAsync(s => s.ID == guild.Id, statsCollection) ?? await Create(guild);
        private static async Task<GuildStats> Create(SocketGuild guild) => await db.InsertAsync(new GuildStats(guild), statsCollection);
    }
}