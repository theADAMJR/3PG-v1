using Bot3PG.DataStructs;
using Bot3PG.Services;
using Discord;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Operations;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Bot3PG.Core.Data
{
    public class DatabaseManager
    {
        public MongoClient MongoClient { get; private set; }
        public IMongoDatabase Database { get; private set; }

        public DatabaseManager() => InitializeDB();

        private void InitializeDB()
        {
            var db = Global.DatabaseConfig;
            var credential = MongoCredential.CreateCredential(db.AuthDatabase, db.User, db.Password);
            //var credential = MongoCredential.CreateCredential("admin", "3PG", "topKek!420");
            var server = new MongoServerAddress(db.Server, db.Port);

            var settings = new MongoClientSettings
            {
                Credential = credential,
                Server = server
            };
            MongoClient = new MongoClient(settings);
            Database = MongoClient.GetDatabase(Global.DatabaseConfig.Database);

            bool connected = Database.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait(1000);
            var log = connected ? (LogSeverity.Info, "Connected to database") : (LogSeverity.Critical, "Cannot connect to database");
            new Task(async () => await LoggingService.LogAsync("Database", log.Item1, log.Item2)).Start();
        }

        public async Task InsertAsync<T>(T item, IMongoCollection<T> collection) => await collection.InsertOneAsync(item);

        public async Task<T> GetAsync<T>(ulong id, IMongoCollection<T> collection)
        {
            try
            {
                var filter = Builders<T>.Filter.Eq("_id", new BsonInt64((long)id));
                var item = await collection.FindAsync(filter);
                return await item.FirstAsync();
            }
            catch
            {
                return default;
            }
        }

        public async Task<List<T>> GetManyAsync<T>(ulong id, IMongoCollection<T> collection)
        {
            try
            {
                var filter = Builders<T>.Filter.Eq("_id", new BsonInt64((long)id));
                var item = await collection.FindAsync(filter);
                return item.ToList();
            }
            catch (Exception error)
            {
                await LoggingService.LogCriticalAsync("Database", error.Message);
                throw error;
            }
        }

        public async Task<List<T>> GetAllAsync<T>(IMongoCollection<T> collection)
        {
            try
            {
                var filter = Builders<T>.Filter.Empty;
                var item = await collection.FindAsync(filter);
                return await item.ToListAsync();
            }
            catch (Exception error)
            {
                await LoggingService.LogCriticalAsync("Database", error.Message);
                throw error;
            }
        }

        public async Task<T> UpdateAsync<T>(ulong id, T newItem, IMongoCollection<T> collection)
        {
            try
            {
                var filter = Builders<T>.Filter.Eq("_id", new BsonInt64((long)id));
                await collection.ReplaceOneAsync(filter, newItem);
                return newItem;
            }
            catch
            {
                return default;
            }
        }

        public async Task DeleteAsync<T>(ulong id, IMongoCollection<T> collection)
        {
            var filter = Builders<T>.Filter.Eq("_id", new BsonInt64((long)id));
            await collection.DeleteOneAsync(filter);
        }

        /*public async Task DeleteManyAsync<T>(T item, IMongoCollection<T> collection)
        {
            var filter = await collection.FindAsync(Builders<T>.Filter.Empty);
            await collection.DeleteManyAsync(filter);
        }*/

        /*public async Task<object> Update<T>(Guid id, string tableName = null)
        {
            var collection = Database.GetCollection<T>(tableName);
            var filter = collection.Find(Builders<T>.Filter.Empty);
            var documents = collection.Find(Builders<T>.Filter.Empty).ToList();
            foreach (var document in documents)
            {
                collection.ReplaceOneAsync(filter, documents);
            }
            return await search.FirstAsync();
        }*/

        public async Task<bool> CheckExistsAsync<T>(ulong id, IMongoCollection<T> collection)
        {
            var item = await collection.FindAsync(new BsonDocument("_id", new BsonInt64((long)id)));
            return item.Any();
        }
    }
}