using System.Threading.Tasks;
using Bot3PG.Data;
using Bot3PG.Data.Structs;
using Bot3PG.Modules.Moderation;
using Discord;
using Discord.WebSocket;
using NUnit.Framework;
using Moq;

namespace Bot3PG.Tests
{
    // MethodName_StateUnderTest_ExpectedBehavior
    // e.g. WithdrawMoney_InvalidAccount_ExceptionThrown

    public class AutoTests : TestModule
    {
        public GuildUser CurrentUser { get; private set; }
        public Guild CurrentGuild { get; private set; }

        public SocketGuild DiscordGuild { get; private set; }
        public SocketGuildUser DiscordUser { get; private set; }
        public DiscordSocketClient Bot { get; private set; } = new DiscordSocketClient();

        [SetUp]
        public async Task Initialize()
        {
            new DiscordService();
            
            CurrentUser = await Users.GetAsync(DiscordUser);
            CurrentGuild = await Guilds.GetAsync(DiscordGuild);
        }

        // [Test]
        public void GlobalConfig_Initialize_ReadsFromFile()
        {
            Assert.AreNotEqual(GlobalConfig.Config?.Token, null);

            Assert.AreNotEqual(DiscordGuild, null);
            Assert.AreNotEqual(CurrentGuild, null);

            Assert.AreEqual(CurrentGuild.ID, DiscordGuild.Id);
            Assert.AreEqual(531196495584821314, DiscordGuild.Id);
        }

        [Test]
        public void GetContentValidation_Explicit_Invalidated()
        {
            var validation = Auto.GetContentValidation(CurrentGuild, "nig", CurrentUser);
            Assert.AreEqual(validation, FilterType.BadWords);
        }
    }

    public class StaffLogsTests
    {
        public StaffLogsTests()
        {
            
        }
    }
}