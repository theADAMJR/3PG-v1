using Bot3PG.DataStructs.Attributes;

namespace Bot3PG.DataStructs
{
    public class ConfigModule
    {
        [Config("Whether this module is enabled. Modules are enabled by default.")]
        public virtual bool Enabled { get; set; } = true;

        public class SubModule : ConfigModule
        {
            private static bool _enabled = true;
            [Config("Whether this module is enabled. Modules are enabled by default.")]
            public override bool Enabled { get => base.Enabled && _enabled; set => _enabled = value; }
        }
    }
}