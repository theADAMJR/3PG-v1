using System;
using Bot3PG.Data;
using Bot3PG.Data.Structs;
using Bot3PG.Handlers;
using NUnit.Framework;

namespace Bot3PG.Testing
{
    public class GuildTests
    {
        // MethodName_StateUnderTest_ExpectedBehavior
        // e.g. WithdrawMoney_InvalidAccount_ExceptionThrown
        [Test]
        public void NewGuild_Null_ExceptionThrown() 
        {
            TestDelegate test = () => new Guild(null);
            Assert.Catch(test);
        }
    }
}