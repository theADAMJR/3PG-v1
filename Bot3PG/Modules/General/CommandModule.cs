using Discord;

namespace Bot3PG.Modules.General
{
    public class CommandModule
    {
        public string Name { get; private set; }
        public Color Color { get; private set; }

        public CommandModule(string name, Color color)
        {
            Name = name;
            Color = color;
        }

        public override string ToString() => Name;
    }
}