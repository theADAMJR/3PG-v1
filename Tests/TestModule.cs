using System;
using System.Threading.Tasks;
using Bot3PG.Data.Structs;
using Discord;
using Discord.WebSocket;
using Moq;
using NUnit.Framework;

namespace Bot3PG.Tests
{
    public class TestModule
    {
        internal IGuildUser DiscordGuildUser { get; private set; }
        internal GuildUser GuildUser { get; private set; }
        internal Guild Guild { get; private set; }

        [SetUp]
        public async Task Initialize()
        {
            DiscordGuildUser = CreateMockGuildUser();
            GuildUser = new GuildUser(DiscordGuildUser);
            Guild = new Guild(DiscordGuildUser.Guild);
        }

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

        internal static IUserMessage CreateMockUserMessage(string content = "")
        {
            var message = new Mock<IUserMessage>();
            message.Setup(u => u.Content).Returns(content);
            message.Setup(u => u.Author).Returns(CreateMockGuildUser());
            message.Setup(u => u.Id).Returns(123);
            message.Setup(u => u.Channel).Returns(CreateMockTextChannel());
            return message.Object;
        }

        internal static ITextChannel CreateMockTextChannel(string name = "general")
        {
            var channel = new Mock<ITextChannel>();
            channel.Setup(c => c.Name).Returns(name.Replace(" ", "-").ToLower());
            channel.Setup(c => c.CreatedAt).Returns(DateTime.Now);
            return channel.Object;
        }
    }
}