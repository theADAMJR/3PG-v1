using System;

namespace Bot3PG.DataStructs.Attributes
{
    public class NotConfigurableAttribute : Attribute
    {
        public bool IsConfigurable { get; private set; }

        public NotConfigurableAttribute() => IsConfigurable = false;
    }
}