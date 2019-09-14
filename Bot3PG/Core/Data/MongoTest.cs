/*using Bot3PG.DataStructs;
using Discord.WebSocket;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Bot3PG.Core.Data
{
    public class MongoTest
    {
        private MongoClient client;
        private IMongoDatabase database;

        public MongoTest() => InitializeDB();

        private void InitializeDB()
        {
            var connectionString = $"mongodb://localhost:27017";
            //$"mongodb+srv://<{Global.DatabaseConfig.Server}>:<{Global.DatabaseConfig.Password}>@<cluster-address>/test?w=majority"
            client = new MongoClient(connectionString);
            database = client.GetDatabase("3pgTest");

            // A collection is like a table within a database
            var catsTable = database.GetCollection<BsonDocument>("cats");
        }

        public async Task<BsonDocument> CreateUserAsync(SocketUser socketUser)
        {
            var collection = database.GetCollection<BsonDocument>("user");

            var newUser = new BsonDocument
            {
                { "_id", BsonInt64.Create(socketUser.Id)},
                { "bannerUrl", "Undefined" },
                { "guilds", new BsonDocument() }
            };

            await collection.InsertOneAsync(new BsonDocument
            {
                { "_id", socketUser.Id.ToString() }
            });

            await collection.InsertOneAsync(newUser);
            return newUser;
        }

        public async Task<BsonDocument> CreateGuildUserAsync(SocketGuildUser socketGuildUser)
        {
            var collection = database.GetCollection<BsonDocument>("user");
            var filter = Builders<BsonDocument>.Filter.Eq("_id", socketGuildUser.Id);
            var socketUser = collection.Find(filter).First();
            //collection.UpdateOne(filter, Users.Get(socketGuildUser));
            return socketUser;
        }

        public async Task CreateGuildAsync(SocketGuild guild)
        {
            var collection = database.GetCollection<BsonDocument>("guild");
            await collection.InsertOneAsync(new BsonDocument
            {
                { "_id", guild.Id.ToString() },
                new Guild(guild).ToBsonDocument()
            });
        }

        public async Task GetUserAsync()
        {
            var collection = database.GetCollection<BsonDocument>("user");

            var filter = Builders<BsonDocument>.Filter.Empty;
            var users = collection.Find(filter).ToList();

            for (int i = 0; i < users.Count; i++)
            {
                Console.WriteLine(users[i].GetValue("_id").ToString());
            }
        }

        private async Task Update()
        {
            var collection = database.GetCollection<BsonDocument>("cats");

            // Update a single document in the inventory collection
            var filter = Builders<BsonDocument>.Filter.Eq("item", "paper");
            var update = Builders<BsonDocument>.Update.Set("size.uom", "cm").Set("status", "P").CurrentDate("lastModified");
            var result = collection.UpdateOne(filter, update);
        }

        private async Task Delete()
        {

        }
    }
}*/