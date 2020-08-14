using System;
using System.Linq;
using System.Threading.Tasks;
using Bot3PG.Modules.Music;
using Bot3PG.Utils;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Moq;
using NUnit.Framework;

namespace Bot3PG.Tests
{
    public class MusicTests : TestModule
    {
        private Music music = new Music();
        private CommandService commands = new CommandService();

        // [Test]
        public async Task Join_NoChannel_ThrowsException()
        {
            var user = CreateMockGuildUser();
            var socketMessage = new Mock<IUserMessage>();
            socketMessage.Setup(m => m.Content).Returns("/music");
            socketMessage.Setup(m => m.Author).Returns(user as IUser);

            int position = 0;
            var context = new SocketCommandContext(Global.Client, socketMessage.Object as SocketUserMessage);            
            var command = commands.Search(context, position).Commands.FirstOrDefault().Command;
            await commands.ExecuteAsync(context, position, null, MultiMatchHandling.Exception);
        }
    }
}