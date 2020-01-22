using Discord;
using Discord.WebSocket;
using Moq;

namespace Bot3PG.Tests
{
    public class TestModule
    {
        internal static Mock<IGuildUser> CreateMockGuildUser(string nickname, string username, string guildName = "MyGuild", string userMention = "@User")
        {
            var guildUser = new Mock<IGuildUser>();
            guildUser.Setup(gUser => gUser.Nickname).Returns(nickname);
            guildUser.Setup(gUser => gUser.Username).Returns(username);
            guildUser.Setup(gUser => gUser.Mention).Returns(userMention);
            guildUser.Setup(gUser => gUser.Guild.Name).Returns(guildName);
            return guildUser;
        }
    }
}