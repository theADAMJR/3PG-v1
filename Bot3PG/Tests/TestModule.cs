using Discord;
using Discord.WebSocket;
using Moq;

namespace Bot3PG.Tests
{
    public class TestModule
    {
        internal static IGuild CreateMockGuild()
        {
            var guild = new Mock<IGuild>();
            guild.Setup(g => g.Name).Returns("Exo");
            return guild.Object;
        }

        internal static IGuildUser CreateMockGuildUser()
        {
            var guildUser = new Mock<IGuildUser>();
            guildUser.Setup(u => u.Nickname).Returns("Adam");
            guildUser.Setup(u => u.Username).Returns("ADAMJR");
            guildUser.Setup(u => u.Guild).Returns(CreateMockGuild());
            return guildUser.Object;
        }
    }
}