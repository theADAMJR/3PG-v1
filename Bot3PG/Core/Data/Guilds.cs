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
        private static readonly IMongoCollection<Guild> collection;
        private const string guildCollection = "guild";

        private static readonly DatabaseManager db;

        // TODO - remove
        public static readonly List<string> ReadOnlyColumns = new List<string>()
        {
            "guild_id"
        };

        static Guilds()
        {
            db = new Lazy<DatabaseManager>().Value;

            collection = db.Database.GetCollection<Guild>(guildCollection);
            if (collection is null)
            {
                db.Database.CreateCollection("user");
                collection = db.Database.GetCollection<Guild>(guildCollection);
            }
        }

        public static async Task<Guild> GetAsync(SocketGuild socketGuild) => await GetOrCreateAsync(socketGuild);

        private static async Task<Guild> GetOrCreateAsync(SocketGuild socketGuild)
        {
            var guild = await db.GetAsync(socketGuild.Id, collection);

            if (guild is null)
            {
                return await CreateGuildAsync(socketGuild);
            }
            return guild;
        }

        private static async Task<Guild> CreateGuildAsync(SocketGuild socketGuild)
        {
            Console.WriteLine("creating guild");
            var newGuild = new Guild(socketGuild);
            await db.InsertAsync(newGuild, collection);
            SetDefaults(socketGuild, newGuild);
            return newGuild;
        }

        public static async Task ResetAsync(SocketGuild socketGuild)
        {
            await DeleteAsync(socketGuild);
            await CreateGuildAsync(socketGuild);
        }

        public static async Task DeleteAsync(SocketGuild socketGuild) => await db.DeleteAsync(socketGuild.Id, collection);

        private static void SetDefaults(SocketGuild socketGuild, Guild newGuild)
        {
            foreach (var textChannel in socketGuild.TextChannels)
            {
                var lowerTextChannelName = textChannel.Name.ToLower();
                if (lowerTextChannelName.Contains("logs"))
                {
                    newGuild.Config.StaffLogsChannel = textChannel;
                }
                if (lowerTextChannelName.Contains("general"))
                {
                    newGuild.Config.AnnounceChannel = textChannel;
                }
                else
                {
                    var announceChannel = socketGuild.SystemChannel ?? socketGuild.DefaultChannel;
                    newGuild.Config.AnnounceChannel = announceChannel;
                }
            }
        }

        public static bool GetConfigColumn(string columnName)
        {
            try
            {
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static List<string> SearchGuildConfigColumns(string columnName)
        {
            try
            {
                return new List<string>();
            }
            catch (Exception ex)
            {
                // TODO - add error source
                throw ex;
            }
        }

        public static void UpdateGuildConfig(SocketGuild socketGuild, string columnName, object newValue)
        {

        }
    }
}