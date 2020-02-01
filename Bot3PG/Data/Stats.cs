using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MongoDB.Driver;

namespace Bot3PG.Data
{
    public static class Stats
    { 
        private static readonly IMongoCollection<GuildStats> statsCollection;

        private const string stats = "user";
        
        private static readonly DatabaseManager db;

        static Stats()
        {
            db = new DatabaseManager(Global.Config.DB);

            var collections = db.Database.ListCollectionNames().ToList();
            
            if (!collections.Any(c => c == stats))
                db.Database.CreateCollection(stats);
            statsCollection = db.Database.GetCollection<GuildStats>(stats);
        }

        public static async Task LogCommandAsync(string name, SocketGuildUser instigator)
        {
            var stats = await Get(instigator.Guild);
            stats.Commands.Append(new CommandStat{ Name = name, InstigatorID = instigator.Id });
        }

        public static async Task<GuildStats> Get(SocketGuild guild) => await GetOrCreate(guild);

        private static async Task<GuildStats> GetOrCreate(SocketGuild guild) => await db.GetAsync(g => g.ID == guild.Id, statsCollection) ?? await Create(guild);
        private static async Task<GuildStats> Create(SocketGuild guild) => await db.InsertAsync(new GuildStats(guild), statsCollection);
    }
}