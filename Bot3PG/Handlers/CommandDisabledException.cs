using System;
using Discord.Commands;

namespace Bot3PG.Handlers
{
    public class CommandDisabledException : Exception 
    {
        public CommandDisabledException(string message = "") : base(message) => message = base.Message;
    }
}