using Discord;
using Discord.Commands;
using System.Collections.Generic;
using System.Linq;

namespace Bot3PG.Modules.General
{
    public class CommandHelp : Dictionary<string, Command>
    {
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
                    usage += !argument.IsOptional ? $" [{argument}]" : $" {argument}";
                }
                var color = Color.Purple;
                for (int i = 0; i < command.Module.Attributes.Count; i++)
                {
                    var colorAttribute = command.Module.Attributes[i] as ColorAttribute;
                    if (colorAttribute is null) continue;
                    color = new Color(colorAttribute.R, colorAttribute.B, colorAttribute.B);
                }

                var commandPermissions = new List<GuildPermission?>();
                Release? release = null;
                for (int i = 0; i < command.Attributes.Count; i++)
                {
                    if (command.Attributes[i] is RequireUserPermissionAttribute userPermissionAttribute)
                    {
                        commandPermissions.Add(userPermissionAttribute.GuildPermission);
                    }
                    if (command.Attributes[i] is ReleaseAttribute releaseAttribute)
                    {
                        release = releaseAttribute.Release;
                    }
                }

                this[command.Name.ToLower()] = new Command(usage, command.Summary, command.Remarks, new CommandModule(command.Module.Name, color), command.Aliases, commandPermissions, release);
            }
        }
    }
}