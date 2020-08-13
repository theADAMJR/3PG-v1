using Discord;

namespace Bot3PG.Data.Structs
{
    public class CommandConfigModule : ConfigModule, IAppearsOnWebapp
    {
        public CommandsSubModule Commands { get; set; } = new CommandsSubModule();

        public class CommandsSubModule : Submodule
        {
            [Config("Modify existing module commands to your servers needs")]
            public CommandOverride[] Overrides { get; set; } = {
                new CommandOverride{ Name = "", Enabled = true, Permission = GuildPermission.Administrator
            }};
        }
    }
}