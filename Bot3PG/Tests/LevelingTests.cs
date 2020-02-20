using System;
using System.Linq;
using Bot3PG.Modules.XP;
using NUnit.Framework;

namespace Bot3PG.Tests
{
    public class LevelingTests : TestModule
    {
        // MethodName_Scenario_ExpectedBehaviour

        [Test]
        public void ValidateForEXPAsync_NullMessage_ThrowsException()
        {
            AsyncTestDelegate act = () => Leveling.ValidateForEXPAsync(null, null);

            Assert.That(act, Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void ValidateForEXPAsync_NullGuild_ThrowsException()
        {
            var message = CreateMockUserMessage();

            AsyncTestDelegate act = () => Leveling.ValidateForEXPAsync(message, null);

            Assert.That(act, Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void ValidateForEXPAsync_UserInCooldown_ThrowsException()
        {
            var message = CreateMockUserMessage();

            GuildUser.XP.LastXPMsg = DateTime.Now;
            AsyncTestDelegate act = () => Leveling.ValidateForEXPAsync(message, Guild);

            Assert.That(act, Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void ValidateForEXPAsync_ExemptChannel_ThrowsException()
        {
            var message = CreateMockUserMessage();

            Guild.XP.ExemptChannels = new ulong[] { 123 };
            AsyncTestDelegate act = () => Leveling.ValidateForEXPAsync(message, null);

            Assert.That(act, Throws.TypeOf<InvalidOperationException>());
        }
    }
}