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
            Leveling.ValidateForEXPAsync();
        }
    }
}