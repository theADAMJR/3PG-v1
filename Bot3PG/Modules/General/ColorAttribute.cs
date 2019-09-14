using Discord;
using System;

namespace Bot3PG.Modules.General
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ColorAttribute : Attribute
    {
        public Color Color { get; private set; }
        public byte R => Color.R;
        public byte G => Color.G;
        public byte B => Color.B;

        public ColorAttribute(byte r, byte g, byte b) => Color = new Color(r, g, b);
        public ColorAttribute(Color color) => Color = new Color(color.R, color.G, color.B);
    }
}