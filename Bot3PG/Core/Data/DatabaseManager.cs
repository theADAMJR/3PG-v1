using Bot3PG.DataStructs;
using Bot3PG.DataStructs.Attributes;
using Bot3PG.Modules.General;
using Bot3PG.Services;
using Discord;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Bot3PG.Core.Data
{
    public class DatabaseManager
    {
        private const string attributes = "attributes";

        public MongoClient MongoClient { get; private set; }
        public IMongoDatabase Database { get; private set; }

        public DatabaseManager() => InitializeDB();

        private void InitializeDB()
        {
            var db = Global.DatabaseConfig;

            MongoClient = new MongoClient($"mongodb://{db.User}:{db.Password}@{db.Server}:{db.Port}/{db.AuthDatabase}");
            Database = MongoClient.GetDatabase(db.Database);

            bool connected = Database.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait(1000);
            var log = connected ? (LogSeverity.Info, $"Connected to database on port {db.Port}") : (LogSeverity.Critical, $"Database connection failed on port {db.Port}");
            new Task(async () => await Debug.LogAsync("Database", log.Item1, log.Item2)).Start();

            var pack = new ConventionPack
            {
                new EnumRepresentationConvention(BsonType.String),
                new IgnoreExtraElementsConvention(true)
            };
            ConventionRegistry.Register("EnumStringConvention", pack, t => true);

            UpdateAttributes();
        }

        private void UpdateAttributes()
        {
            var guildMirror = new Dictionary<string, dynamic>{}.ToBsonDocument();
            guildMirror["_id"] = "Attributes";

            foreach (var property in typeof(Guild).GetProperties())
            {
                var module = property.Name;
                if (property.PropertyType.BaseType == typeof(ConfigModule) && property.PropertyType != typeof(ConfigModule.SubModule))
                {
                    guildMirror[module] = new BsonDocument();
                    guildMirror[module]["Config"] = GetConfig(property)?.ToBsonDocument() ?? new BsonDocument();
                    foreach (var subProperty in property.PropertyType.GetProperties())
                    {
                        guildMirror[module][subProperty.Name] = new BsonDocument();
                        guildMirror[module][subProperty.Name]["Config"] = GetConfig(subProperty)?.ToBsonDocument() ?? new BsonDocument();
                    }
                }
                else if (property.PropertyType.BaseType == typeof(ConfigModule)) // TODO => fix
                {
                    var submoduleProperties = property.PropertyType.GetProperties();
                    foreach (var subProperty in submoduleProperties)
                    {
                        guildMirror[module][subProperty.Name][property.Name] = new BsonDocument
                        {
                            ["Config"] = GetConfig(subProperty)?.ToBsonDocument() ?? new BsonDocument()
                        };
                    }
                }
                else
                {
                    guildMirror[property.Name] = GetConfig(property)?.ToBsonDocument() ?? new BsonDocument();
                }
            }
            var collections = Database.ListCollectionNames().ToList();
            if (!collections.Any(c => c == attributes))
            {
                Database.CreateCollection(attributes);
            }
            var collection = Database.GetCollection<BsonDocument>(attributes);

            bool isSaved = collection.FindSync(d => d["_id"] == "Attributes").Any();
            if (!isSaved)
            {
                collection.InsertOne(guildMirror);
            }
            else
            {
                collection.ReplaceOne(d => d["_id"] == "Attributes", guildMirror);
            }
        }
        private static ConfigAttribute GetConfig(PropertyInfo propertyInfo) => propertyInfo.GetCustomAttributes(attributeType: typeof(ConfigAttribute), false).FirstOrDefault() as ConfigAttribute;

        public async Task UpdateCommands(CommandHelp commandHelp)
        {
            var collection = Database.GetCollection<BsonDocument>(attributes);
            var commandMirror = new BsonDocument();
            commandMirror["_id"] = "Commands";
            try
            {
                commandMirror["Commands"] = commandHelp.ToBsonDocument();
            }
            catch (Exception err)
            {
                await Debug.LogAsync("database", LogSeverity.Error, err);
            }

            var commands = await collection.FindAsync(d => d["_id"] == "Commands");
            if (!commands.Any())
            {
                collection.InsertOne(commandMirror);
            }
            else
            {
                collection.ReplaceOne(d => d["_id"] == "Commands", commandMirror);
            }
        }

        public async Task InsertAsync<T>(T item, IMongoCollection<T> collection)
        {
            try
            {
                await collection.InsertOneAsync(item);
            }
            catch {}
        }

        public async Task<T> GetAsync<T>(Expression<Func<T, bool>> predicate, IMongoCollection<T> collection)
        {
            var result = await collection.FindAsync(predicate);
            return await result.FirstOrDefaultAsync();
        }

        public async Task<List<T>> GetManyAsync<T>(Expression<Func<T, bool>> predicate, IMongoCollection<T> collection)
        {
            try
            {
                var items = await collection.FindAsync(predicate);
                return await items.ToListAsync();
            }
            catch { return default; }
        }

        public async Task<T> UpdateAsync<T>(Expression<Func<T, bool>> predicate, T newItem, IMongoCollection<T> collection)
        {
            try
            {
                await collection.ReplaceOneAsync(predicate, newItem);
                return newItem;
            }
            catch (Exception ex)
            {
                await Debug.LogErrorAsync("Database", "UpdateAsync() -> Could not update", ex);
                return default;
            }
        }

        public async Task DeleteAsync<T>(Expression<Func<T, bool>> predicate, IMongoCollection<T> collection)
        {
            try
            {
                await collection.DeleteOneAsync(predicate);
            }
            catch (Exception ex)
            {
                await Debug.LogErrorAsync("Database", "DeleteAsync() -> Could not delete", ex);
            }
        }

        public async Task<bool> CheckExistsAsync<T>(Expression<Func<T, bool>> predicate, IMongoCollection<T> collection) => await (await collection.FindAsync(predicate)).AnyAsync();
    }
}