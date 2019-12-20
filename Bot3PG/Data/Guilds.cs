using Bot3PG.Data.Structs;
using Discord.WebSocket;
using MongoDB.Driver;
using System.Linq;
using System.Threading.Tasks;

namespace Bot3PG.Data
{
    public static class Guilds
    {
        public static readonly IMongoCollection<Guild> collection;
        private const string guildCollection = "guild";

        private static readonly DatabaseManager db;

        static Guilds()
        {
            db = new DatabaseManager();

            var collections = db.Database.ListCollectionNames().ToList();
            if (!collections.Any(c => c == guildCollection))
            {
                db.Database.CreateCollection(guildCollection);
            }
            collection = db.Database.GetCollection<Guild>(guildCollection);
        }

        public static async Task Save(Guild guild) => await db.UpdateAsync(g => g.ID == guild.ID, guild, collection);

        public static async Task<Guild> GetAsync(SocketGuild socketGuild) => await GetOrCreateAsync(socketGuild);

        private static async Task<Guild> GetOrCreateAsync(SocketGuild socketGuild)
        {
            if (socketGuild is null) return null;

            var guild = await db.GetAsync(g => g.ID == socketGuild.Id, collection);
            return guild ?? await CreateGuildAsync(socketGuild);
        }

        private static async Task<Guild> CreateGuildAsync(SocketGuild socketGuild)
        {
            var newGuild = new Guild(socketGuild);
            
            try { await db.InsertAsync(newGuild, collection); }
            catch { await db.UpdateAsync(g => g.ID == socketGuild.Id, newGuild, collection); }
            await SetDefaults(socketGuild, newGuild);

            return newGuild;
        }

        public static async Task ResetAsync(SocketGuild socketGuild)
        {
            await DeleteAsync(socketGuild);
            await CreateGuildAsync(socketGuild);
        }

        public static async Task DeleteAsync(SocketGuild socketGuild) => await db.DeleteAsync(g => g.ID == socketGuild.Id, collection);

        private static async Task SetDefaults(SocketGuild socketGuild, Guild guild)
        {
            foreach (var textChannel in socketGuild.TextChannels)
            {
                string lowerTextChannelName = textChannel.Name.ToLower();
                if (lowerTextChannelName.Contains("logs"))
                {
                    guild.Moderation.StaffLogs.Channel = textChannel.Id;
                }
                if (lowerTextChannelName.Contains("general"))
                {
                    guild.General.Announce.Channel = textChannel.Id;
                }
                else
                {
                    var announceChannel = socketGuild.SystemChannel ?? socketGuild.DefaultChannel;
                    guild.General.Announce.Channel = announceChannel.Id;

                    var agreeRole = socketGuild.Roles.FirstOrDefault(r => r.Name == "Member");
                    if (agreeRole != null)
                    {
                        guild.Admin.Rulebox.Role = agreeRole.Id;
                    }
                }
            }
            await Save(guild);
        }
    }
}