using Discord;
using MongoDB.Bson.Serialization.Attributes;

namespace Bot3PG.Modules
{
    public struct CommandModule
    {
        public string Name { get; private set; }
        [BsonIgnore] public Color Color { get; private set; }

        public CommandModule(string name, Color color) { Name = name; Color = color; }

        public override string ToString() => Name;
    }
}