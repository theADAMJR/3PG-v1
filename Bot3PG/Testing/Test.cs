using System;
using Bot3PG.Data;
using Bot3PG.Data.Structs;
using Bot3PG.Handlers;
using NUnit.Framework;

namespace Bot3PG.Testing
{
    public class Test
    {
        public void ConnectToDatabase() 
        {
            var config = GlobalConfig.Config;
            new Global(null, null, config, null);
        }

        [Test]
        public void Test1() 
        {
            Assert.AreSame(1, "1");
            //new CommandHandler();
        }
    }
}