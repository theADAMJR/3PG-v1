using Discord;
using Discord.WebSocket;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Bot3PG.Data.Structs
{
    public class LogEventProperties
    {
        [Config("The event to log"), Dropdown(typeof(LogEvent))]
        public LogEvent LogEvent { get; set; }

        [Config("The colour of the message embed"), SpecialType(typeof(Color))] 
        public string Colour { get; set; }

        [BsonRepresentation(BsonType.String), Config("The text channel of the log message"), SpecialType(typeof(SocketTextChannel))] 
        public ulong Channel { get; set; }
    }
}