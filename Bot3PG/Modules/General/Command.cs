using Discord;
using System.Collections.Generic;

namespace Bot3PG.Modules.General
{
    public class Command
    {
        public string Usage { get; private set; }
        public string Summary { get; private set; }
        public string Remarks { get; private set; }
        public CommandModule Module { get; private set; }
        public IReadOnlyList<string> Alias { get; private set; }
        public List<GuildPermission?> RequiredPermissions { get; private set; }
        public Release? Release { get; private set; }

        public Command(string usage, string summary, string remarks, CommandModule module, IReadOnlyList<string> alias, List<GuildPermission?> requiredPermissions, Release? release)
        {
            Usage = usage;
            Summary = summary;
            Remarks = remarks;
            Module = module;
            Alias = alias;
            RequiredPermissions = requiredPermissions;
            Release = release;
        }
    }
}