using Bot3PG.DataStructs;
using Discord.WebSocket;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bot3PG.Core.Data
{
    public static class Guilds
    {
        public static IMongoCollection<Guild> Collection { get; private set; }
        private const string guildCollection = "guild";

        private static readonly DatabaseManager db;
        static Guilds()
        {
            db = new DatabaseManager();

            Collection = db.Database.GetCollection<Guild>(guildCollection);
            if (Collection is null)
            {
                db.Database.CreateCollection("user");
                Collection = db.Database.GetCollection<Guild>(guildCollection);
            }
        }

        public static async Task Save(Guild guild) => await db.UpdateAsync(guild.ID, guild, Collection);

        public static async Task<Guild> GetAsync(SocketGuild socketGuild) => await GetOrCreateAsync(socketGuild);

        private static async Task<Guild> GetOrCreateAsync(SocketGuild socketGuild)
        {
            var guild = await db.GetAsync(socketGuild.Id, Collection);

            if (guild is null)
            {
                return await CreateGuildAsync(socketGuild);
            }
            return guild;
        }

        private static async Task<Guild> CreateGuildAsync(SocketGuild socketGuild)
        {
            var newGuild = new Guild(socketGuild);
            await db.InsertAsync(newGuild, Collection);
            SetDefaults(socketGuild, newGuild);
            return newGuild;
        }

        public static async Task ResetAsync(SocketGuild socketGuild)
        {
            await DeleteAsync(socketGuild);
            await CreateGuildAsync(socketGuild);
        }

        public static async Task DeleteAsync(SocketGuild socketGuild) => await db.DeleteAsync(socketGuild.Id, Collection);

        private static void SetDefaults(SocketGuild socketGuild, Guild newGuild)
        {
            foreach (var textChannel in socketGuild.TextChannels)
            {
                var lowerTextChannelName = textChannel.Name.ToLower();
                if (lowerTextChannelName.Contains("logs"))
                {
                    newGuild.Moderation.StaffLogs.Channel = textChannel;
                }
                if (lowerTextChannelName.Contains("general"))
                {
                    newGuild.General.Announce.Channel = textChannel;
                }
                else
                {
                    var announceChannel = socketGuild.SystemChannel ?? socketGuild.DefaultChannel;
                    newGuild.General.Announce.Channel = announceChannel;
                }
            }
        }
    }
}