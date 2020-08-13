namespace Bot3PG.Data.Structs
{
    public class ConfigModule
    {
        [Config("Whether this module is enabled. Most modules are enabled by default.")]
        public virtual bool Enabled { get; set; } = true;

        public virtual bool IsAllowed(bool condition) => Enabled && condition;

        public class Submodule : ConfigModule
        {
            [Config("Whether this submodule is enabled. Most submodules are enabled by default.")]
            public override bool Enabled { get; set; } = true;
            
            public override bool IsAllowed(bool condition) => base.Enabled && this.Enabled && condition;
        }
    }
}