using Discord.Commands;

namespace Bot3PG.Handlers
{
    public struct CommandValidation 
    { 
        public SearchResult Search { get; set; }
        public CommandInfo Command { get; set; }
    }
}