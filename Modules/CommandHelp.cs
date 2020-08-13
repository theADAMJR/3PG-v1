using Bot3PG.Data;
using Discord;
using Discord.Commands;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
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
                bool requiresOwner = command.Preconditions.Any(p => p is RequireOwnerAttribute) || command.Module.Preconditions.Any(p => p is RequireOwnerAttribute);
                if (requiresOwner) continue;

                string usage = GetUsage(command);
                var colour = GetColour(command);
                var preconditions = GetPreconditions(command);

                this[command.Name.ToLower()] = new Command(usage, command.Summary, command.Remarks, new CommandModule(command.Module.Name, colour), command.Aliases, preconditions);
            }
        }

        public static string GetUsage(CommandInfo command, string alias = null)
        {
            if (command is null) 
                throw new ArgumentNullException("Command cannot be null");

            string usage = alias?.ToLower() ?? command.Name.ToLower();
            for (int i = 0; i < command.Parameters.Count; i++)
            {
                var argument = command.Parameters[i];
                string defaultValue = !string.IsNullOrEmpty(argument.DefaultValue?.ToString()) ? $" = {argument.DefaultValue}" : "";
                usage += argument.IsOptional ? $" [{argument}{defaultValue}]" : $" {argument}";
            }
            return usage;
        }

        private static Color GetColour(CommandInfo command)
        {
            var colour = Color.Purple;
            for (int i = 0; i < command.Module.Attributes.Count; i++)
            {
                var colorAttribute = command.Module.Attributes[i] as ColorAttribute;
                if (colorAttribute is null) continue;

                colour = new Color(colorAttribute.R, colorAttribute.B, colorAttribute.B);
            }
            return colour;
        }

        private static List<GuildPermission?> GetPreconditions(CommandInfo command)
        {
            var preconditions = new List<GuildPermission?>();
            for (int i = 0; i < command.Preconditions.Count; i++)
            {
                if (command.Preconditions[i] is RequireUserPermissionAttribute userPermissionAttribute)
                    preconditions.Add(userPermissionAttribute.GuildPermission);
            }
            for (int i = 0; i < command.Module.Preconditions.Count; i++)
            {
                if (command.Module.Preconditions[i] is RequireUserPermissionAttribute userPermissionAttribute)
                {
                    if (!preconditions.Contains(userPermissionAttribute.GuildPermission))
                        preconditions.Add(userPermissionAttribute.GuildPermission);
                }
            }

            return preconditions;
        }
    }
}