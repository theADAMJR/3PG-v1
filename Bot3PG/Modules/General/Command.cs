using Discord;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using System.Linq;

namespace Bot3PG.Modules.General
{
    public class Command
    {
        public string Usage { get; private set; }
        public string Summary { get; private set; }
        public string Remarks { get; private set; }
        public CommandModule Module { get; private set; }
        public List<string> Alias { get; private set; }
        public List<GuildPermission?> Preconditions { get; private set; }
        public Release? Release { get; private set; }

        public Command(string usage, string summary, string remarks, CommandModule module, IReadOnlyList<string> alias, List<GuildPermission?> preconditions, Release? release)
        {
            Usage = usage;
            Summary = summary;
            Remarks = remarks;
            Module = module;
            Alias = alias.ToList();
            Preconditions = preconditions;
            Release = release;
        }
    }
}