using Bot3PG.Data;
using Discord;
using Discord.Commands;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;

namespace Bot3PG.Modules
{
    public class CommandHelp : Dictionary<string, Command>
    {
        [BsonRepresentation(BsonType.Array)]
        public HashSet<CommandModule> Modules => Values.Select(command => command.Module).Distinct().ToHashSet();

        public CommandHelp()
        {
            var commandService = Global.CommandService;
            foreach (var command in commandService.Commands)
            {
                string usage = command.Name.ToLower();
                for (int i = 0; i < command.Parameters.Count; i++)
                {
                    var argument = command.Parameters[i];
                    usage += argument.IsOptional ? $" [{argument}]" : $" {argument}";
                }
                var color = Color.Purple;
                for (int i = 0; i < command.Module.Attributes.Count; i++)
                {
                    var colorAttribute = command.Module.Attributes[i] as ColorAttribute;
                    if (colorAttribute is null) continue;
                    color = new Color(colorAttribute.R, colorAttribute.B, colorAttribute.B);
                }

                var preconditions = new List<GuildPermission?>();
                for (int i = 0; i < command.Preconditions.Count; i++)
                {
                    if (command.Preconditions[i] is RequireUserPermissionAttribute userPermissionAttribute)
                    {
                        preconditions.Add(userPermissionAttribute.GuildPermission);
                    }
                }

                this[command.Name.ToLower()] = new Command(usage, command.Summary, command.Remarks, new CommandModule(command.Module.Name, color), command.Aliases, preconditions);
            }
        }
    }
}