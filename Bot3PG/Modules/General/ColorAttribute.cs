using Discord;
using System;

namespace Bot3PG.Modules.General
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ColorAttribute : Attribute
    {
        private readonly Color color;

        public ColorAttribute(byte r, byte g, byte b) => color = new Color(r, g, b);
        public ColorAttribute(Color color) => this.color = new Color(color.R, color.G, color.B);

        public Color Color => color;
        public byte R => color.R;
        public byte G => color.G;
        public byte B => color.B;
    }
}