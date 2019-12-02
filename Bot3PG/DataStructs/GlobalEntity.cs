using Bot3PG.DataStructs.Attributes;
using MongoDB.Bson.Serialization.Attributes;

namespace Bot3PG.DataStructs
{
    public abstract class GlobalEntity<T>
    {
        [NotConfigurable]
        [BsonIgnore] internal static T _id;
        [NotConfigurable]
        [BsonId] public T ID { get => _id; internal set => _id = value; }
    }
}