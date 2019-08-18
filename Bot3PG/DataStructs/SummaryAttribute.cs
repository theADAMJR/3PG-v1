using System;

namespace Bot3PG.DataStructs
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    public sealed class DescriptionAttribute : Attribute
    {
        public string Description { get; private set; }

        public DescriptionAttribute(string positionalString)
        {
            Description = positionalString;
        }
    }
}