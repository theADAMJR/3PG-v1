using Bot3PG.Modules;
using Discord;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Bot3PG.DataStructs.Attributes
{
    public enum InputType { Default, Color, Range }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class ConfigAttribute : Attribute
    {
        public string Colour { get; set; }
        public string Description { get; private set; }
        public bool IsPremium { get; private set; }
        public Release Release { get; private set; }
        public InputType InputType { get; private set; }

        public ConfigAttribute(string description, bool isPremium = false, Release release = Release.Stable, InputType inputType = InputType.Default, string colour = null)
        {
            Description = description ?? "No description set.";
            Colour = colour ?? Color.Purple.ToString();
            IsPremium = isPremium;
            Release = release;
            InputType = inputType;
        }

        public ConfigAttribute() {}
    }
}