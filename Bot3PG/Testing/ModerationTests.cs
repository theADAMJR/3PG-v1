using System.Threading.Tasks;
using Bot3PG.Data;
using Bot3PG.Data.Structs;
using Bot3PG.Modules.Moderation;
using Discord;
using Discord.WebSocket;
using NUnit.Framework;

namespace Bot3PG.Testing
{
    // MethodName_StateUnderTest_ExpectedBehavior
    // e.g. WithdrawMoney_InvalidAccount_ExceptionThrown

    public class AutoTests
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
            /*new Global(Bot, null, GlobalConfig.Config, null);
            new DatabaseManager(GlobalConfig.Config.DB);

            await Bot.LoginAsync(TokenType.Bot, GlobalConfig.Config.Token);
            await Task.Delay(1000); // wait for bot to login*/

            // DiscordGuild = Global.Client.GetGuild(531196495584821314);
            // System.Console.WriteLine(Bot.Guilds.Count);
            // DiscordUser = DiscordGuild.CurrentUser;

            CurrentUser = await Users.GetAsync(DiscordUser);
            CurrentGuild = await Guilds.GetAsync(DiscordGuild);
        }

        [Test]
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

        [Test]
        public async Task ValidateLogChannel_Null_ExceptionThrown()
        {
            // await Validate
        }
    }
}