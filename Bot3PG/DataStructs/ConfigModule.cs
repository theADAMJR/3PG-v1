using Discord;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Bot3PG.DataStructs
{
    public class ConfigModule
    {
        // TODO - make colour serializable
        [BsonIgnore] public Color ModuleColor { get; set; } = Color.DarkPurple;
        public bool Enabled { get; set; } = true;

        public class SubModule : ConfigModule
        {
            
        }
    }
}