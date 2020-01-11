using System;
using System.Threading.Tasks;
using Bot3PG.Data;
using Bot3PG.Data.Structs;
using Bot3PG.Handlers;
using Bot3PG.Modules.XP;
using Discord.WebSocket;
using NUnit.Framework;

namespace Bot3PG.Testing
{
    public class GuildTests
    {
        // MethodName_StateUnderTest_ExpectedBehavior
        // e.g. WithdrawMoney_InvalidAccount_ExceptionThrown

        // Data
        [Test]
        public void NewGuild_Null_ExceptionThrown() 
        {
            TestDelegate test = () => new Guild(null);
            Assert.Catch(test);
        }

        [Test]
        public void NewGuildUser_Null_ExceptionThrown()
        {
            TestDelegate test = () => new GuildUser(null);
            Assert.Catch(test);
        }

        [Test]
        public async Task GetGuild_Null_NullReturned()
        {
            var guild = await Guilds.GetAsync(null);
            Assert.AreEqual(guild, null);
        }

        [Test]
        public async Task GetUser_Null_NullReturned()
        {
            var user = await Users.GetAsync(null as SocketUser);
            Assert.AreEqual(user, null);
        }

        [Test]
        public async Task GetGuildUser_Null_NullReturned()
        {
            var user = await Users.GetAsync(null);
            Assert.AreEqual(user, null);
        }

        // Module
        [Test]
        public void MessageOrGuild_Null_ExceptionThrown()
        {
            AsyncTestDelegate test = async() => await Leveling.ValidateCanEarnEXP(null, null);
            Assert.CatchAsync(test);
        }
    }
}