using Bot3PG.Data.Structs;
using Bot3PG.Modules;
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

namespace Bot3PG.Data
{
    public class DatabaseManager
    {
        private const string attributes = "attributes";

        public MongoClient MongoClient { get; private set; }
        public IMongoDatabase Database { get; private set; }

        public DatabaseManager(Config.DatabaseConfig db) => InitializeDB(db);

        private void InitializeDB(Config.DatabaseConfig db)
        {
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

        private void UpdateAttributes() => SaveGuildMirror(BuildGuildMirror());

        private void SaveGuildMirror(BsonDocument guildMirror)
        {
            var collections = Database.ListCollectionNames().ToList();
            if (!collections.Any(c => c == attributes))
                Database.CreateCollection(attributes);

            var collection = Database.GetCollection<BsonDocument>(attributes);

            bool isSaved = collection.FindSync(d => d["_id"] == "Attributes").Any();
            if (!isSaved)
                collection.InsertOne(guildMirror);
            else
                collection.ReplaceOne(d => d["_id"] == "Attributes", guildMirror);
        }

        private static BsonDocument BuildGuildMirror()
        {
            var guildMirror = new Dictionary<string, dynamic>{}.ToBsonDocument();
            guildMirror["_id"] = "Attributes";
            foreach (var property in typeof(Guild).GetProperties())
            {
                bool isModule = property.PropertyType.BaseType == typeof(ConfigModule) || property.PropertyType.BaseType.BaseType == typeof(ConfigModule);
                if (!isModule) continue;

                string module = property.Name;
                guildMirror[module] = new BsonDocument();
                guildMirror[module]["Config"] = GetConfig(property);
                guildMirror[module]["Type"] = property.PropertyType.ToString();
                guildMirror[module]["SpecialType"] = GetSpecialType(property);

                guildMirror[module]["Type"] = "Module";
                foreach (var modProp in property.PropertyType.GetProperties())
                {
                    guildMirror[module][modProp.Name] = new BsonDocument();
                    guildMirror[module][modProp.Name]["Config"] = GetConfig(modProp);
                    guildMirror[module][modProp.Name]["Type"] = modProp.PropertyType.ToString();
                    guildMirror[module][modProp.Name]["SpecialType"] = GetSpecialType(modProp);

                    if (modProp.PropertyType.IsArray)
                    {
                        var arrayTypeProps = modProp.PropertyType.GetElementType().GetProperties();
                        foreach (var arrayProp in arrayTypeProps)
                        {
                            guildMirror[module][modProp.Name][arrayProp.Name] = new BsonDocument();
                            guildMirror[module][modProp.Name][arrayProp.Name]["Config"] = GetConfig(arrayProp);
                            guildMirror[module][modProp.Name][arrayProp.Name]["Type"] = arrayProp.PropertyType.ToString();
                            guildMirror[module][modProp.Name][arrayProp.Name]["SpecialType"] = GetSpecialType(arrayProp);
                        }
                    }
                    if (modProp.PropertyType.BaseType != typeof(ConfigModule.Submodule)) continue;

                    guildMirror[module][modProp.Name]["Type"] = "Submodule";
                    foreach (var submodProp in modProp.PropertyType.GetProperties())
                    {
                        guildMirror[module][modProp.Name][submodProp.Name] = new BsonDocument();
                        guildMirror[module][modProp.Name][submodProp.Name]["Config"] = GetConfig(submodProp);
                        guildMirror[module][modProp.Name][submodProp.Name]["Type"] = submodProp.PropertyType.ToString();
                        guildMirror[module][modProp.Name][submodProp.Name]["SpecialType"] = GetSpecialType(submodProp);

                        if (submodProp.PropertyType.IsArray)
                        {
                            var arrayTypeProps = submodProp.PropertyType.GetElementType().GetProperties();
                            foreach (var arrayProp in arrayTypeProps)
                            {
                                guildMirror[module][modProp.Name][submodProp.Name][arrayProp.Name] = new BsonDocument();
                                guildMirror[module][modProp.Name][submodProp.Name][arrayProp.Name]["Config"] = GetConfig(arrayProp);
                                guildMirror[module][modProp.Name][submodProp.Name][arrayProp.Name]["Type"] = arrayProp.PropertyType.ToString();
                                guildMirror[module][modProp.Name][submodProp.Name][arrayProp.Name]["SpecialType"] = GetSpecialType(arrayProp);
                            }
                        }
                        else if (submodProp.PropertyType.IsClass)
                        {
                            foreach (var subname in submodProp.PropertyType.GetProperties())
                            {
                                guildMirror[module][modProp.Name][submodProp.Name][subname.Name] = new BsonDocument();
                                guildMirror[module][modProp.Name][submodProp.Name][subname.Name]["Config"] = GetConfig(subname);
                                guildMirror[module][modProp.Name][submodProp.Name][subname.Name]["Type"] = subname.PropertyType.ToString();
                                guildMirror[module][modProp.Name][submodProp.Name][subname.Name]["SpecialType"] = GetSpecialType(subname);
                            }
                        }
                    }
                }
            }
            return guildMirror;
        }

        private static BsonDocument GetConfig(PropertyInfo propertyInfo) => GetConfigAttribute(propertyInfo).ToBsonDocument() ?? new BsonDocument();
        private static ConfigAttribute GetConfigAttribute(PropertyInfo propertyInfo) 
            => propertyInfo.GetCustomAttributes(attributeType: typeof(ConfigAttribute), false).FirstOrDefault() as ConfigAttribute;

        private static BsonDocument GetSpecialType(PropertyInfo propertyInfo) => GetSpecialTypeAttribute(propertyInfo).ToBsonDocument() ?? new BsonDocument();
        private static SpecialTypeAttribute GetSpecialTypeAttribute(PropertyInfo propertyInfo) 
            => propertyInfo.GetCustomAttributes(attributeType: typeof(SpecialTypeAttribute), false).FirstOrDefault() as SpecialTypeAttribute;

        public async Task UpdateCommands(CommandHelp commandHelp)
        {
            var collection = Database.GetCollection<BsonDocument>(attributes);
            var commandMirror = new BsonDocument();
            commandMirror["_id"] = "Commands";
            try
            {
                commandMirror["Commands"] = commandHelp.ToBsonDocument();

                var configModules = typeof(Guild).GetProperties().Where(p => p.PropertyType.GetInterface(nameof(IAppearsOnWebapp)) != null);
                commandMirror["Modules"] = new BsonArray(configModules.Select(p => p.Name));
            }
            catch (Exception err) { await Debug.LogAsync("database", LogSeverity.Error, err); }

            var commands = await collection.FindAsync(d => d["_id"] == "Commands");
            if (!commands.Any())
                collection.InsertOne(commandMirror);
            else
                collection.ReplaceOne(d => d["_id"] == "Commands", commandMirror);
        }

        public async Task InsertAsync<T>(T item, IMongoCollection<T> collection)
        {
            try { await collection.InsertOneAsync(item); }
            catch {}
        }

        public async Task<T> GetAsync<T>(Expression<Func<T, bool>> predicate, IMongoCollection<T> collection)
        {
            try
            {
                var result = await collection.FindAsync(predicate);
                return await result.FirstOrDefaultAsync();           
            }
            catch (Exception ex)
            {
                await Debug.LogErrorAsync("db", ex.Message, ex);
                return default;
            }
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
            try { await collection.DeleteOneAsync(predicate); }
            catch (Exception ex) { await Debug.LogErrorAsync("Database", "DeleteAsync() -> Could not delete", ex); }
        }

        public async Task<bool> CheckExistsAsync<T>(Expression<Func<T, bool>> predicate, IMongoCollection<T> collection) => await (await collection.FindAsync(predicate)).AnyAsync();
    }
}