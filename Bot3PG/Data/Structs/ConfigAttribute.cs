using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Bot3PG.Data.Structs
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class ConfigAttribute : Attribute
    {
        public string Description { get; private set; }
        public bool IsPremium { get; private set; }        
        public string ExtraInfo { get; set; }

        public ConfigAttribute(string description, bool isPremium = false, string extraInfo = null)
        {
            Description = description ?? "No description set.";
            IsPremium = isPremium;
            ExtraInfo = extraInfo;
        }
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class SpecialTypeAttribute : Attribute
    {
        public string Type { get; private set; }
        public SpecialTypeAttribute(Type type) => Type = type.ToString();
        public SpecialTypeAttribute() {}
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class DropdownAttribute : SpecialTypeAttribute
    {
        public List<string> DropdownOptions { get; set; } = new List<string>();

        public DropdownAttribute(Type type) : base(type)
        {
            if (type.IsEnum)
            {
                foreach (var value in Enum.GetValues(type))
                {
                    DropdownOptions.Add(value.ToString());
                }
            }
        }
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class ListAttribute : SpecialTypeAttribute
    {
        public List<string> ListOptions { get; set; } = new List<string>();

        public ListAttribute(Type type) : base(type)
        {
            if (type.IsEnum)
            {
                foreach (var value in Enum.GetValues(type))
                {
                    ListOptions.Add(value.ToString());
                }
            }
        }
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class RangeAttribute : SpecialTypeAttribute
    {
        public float Min { get; set; }
        public float Max { get; set; }

        public RangeAttribute(int min, int max) { Min = min; Max = max; }
        public RangeAttribute(float min, float max) { Min = min; Max = max; }
    }
}