using Discord;
using Discord.Commands;
using System.Collections.Generic;

namespace Bot3PG.Modules.General
{
    public class CommandHelp : Dictionary<string, Command>
    {
        public CommandHelp(IDictionary<string, Command> dictionary, CommandService commandService) : base(dictionary)
        {
            foreach (var command in commandService.Commands)
            {
                string usage = command.Name.ToLower();
                for (int i = 0; i < command.Parameters.Count; i++)
                {
                    var argument = command.Parameters[i];
                    usage += !argument.IsOptional ? $" [{argument}]" : $" {argument.ToString()}";
                }
                var color = Color.Purple;
                for (int i = 0; i < command.Module.Attributes.Count; i++)
                {
                    var colorAttribute = command.Module.Attributes[i] as ColorAttribute;
                    if (colorAttribute is null) continue;
                    color = new Color(colorAttribute.R, colorAttribute.B, colorAttribute.B);
                }
                this[command.Name.ToLower()] = new Command(usage, command.Summary, command.Remarks, new CommandModule(command.Module.Name, color), command.Aliases, null);
            }
        }
    }
}