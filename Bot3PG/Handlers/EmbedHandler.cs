using Discord;
using System.Threading.Tasks;

namespace Bot3PG.Handlers
{
    public static class EmbedHandler
    {
        public static async Task<Embed> CreateBasicEmbed(string title, string description, Color color)
        {
            var embed = await Task.Run(() => (new EmbedBuilder()
                .WithTitle(title)
                .WithDescription(description)
                .WithColor(color)
                .WithCurrentTimestamp().Build()));
            return embed;
        }
        public static async Task<Embed> CreateSimpleEmbed(string title, string description, Color color)
        {
            var embed = await Task.Run(() => (new EmbedBuilder()
                .WithTitle(title)
                .WithDescription(description)
                .WithColor(color).Build()));
            return embed;
        }

        public static async Task<Embed> CreateErrorEmbed(string source, string error)
        {
            var embed = await Task.Run(() => new EmbedBuilder()
                .WithTitle($"{source}")
                .WithColor(Color.DarkRed)
                .WithDescription(error)
                .WithCurrentTimestamp().Build());
            return embed;
        }
    }
}