using System;
using System.Threading.Tasks;
using Bot3PG.Utils;
using Discord;
using Discord.WebSocket;
using Moq;
using NUnit.Framework;

namespace Bot3PG.Tests
{
    public class CommandUtilsTests : TestModule
    {
        [Test]
        public void ParseDuration_NegativeOrForever_ReturnsMaxTimeSpan()
        {
            // Arrange
            string duration1 = "-1";
            string duration2 = "forever";
            string duration3 = "";

            // Act
            var expected = TimeSpan.MaxValue;
            var result1 = CommandUtils.ParseDuration(duration1);
            var result2 = CommandUtils.ParseDuration(duration2);
            var result3 = CommandUtils.ParseDuration(duration3);

            // Assert
            Assert.AreEqual(result1, expected);
            Assert.AreEqual(result2, expected);
            Assert.AreEqual(result3, expected);
        }
        
        [Test]
        public void ParseDuration_InvalidFormat_ThrowsException()
        {
            string invalid1 = "1";
            string invalid2 = "-2";

            TestDelegate result1 = () => CommandUtils.ParseDuration(invalid1);
            TestDelegate result2 = () => CommandUtils.ParseDuration(invalid2);

            Assert.Throws(typeof(ArgumentException), result1);
            Assert.Throws(typeof(ArgumentException), result2);
        }
        
        [Test]
        public void ParseDuration_ValidFormat_ReturnsTimeSpan()
        {
            var seconds = CommandUtils.ParseDuration("7s");
            var minutes = CommandUtils.ParseDuration("7m");
            var hours = CommandUtils.ParseDuration("7h");
            var days = CommandUtils.ParseDuration("7d");
            var weeks = CommandUtils.ParseDuration("7w");
            var months = CommandUtils.ParseDuration("7mo");
            var years = CommandUtils.ParseDuration("7y");

            Assert.AreEqual(seconds, TimeSpan.FromSeconds(7));
            Assert.AreEqual(minutes, TimeSpan.FromMinutes(7));
            Assert.AreEqual(hours, TimeSpan.FromHours(7));
            Assert.AreEqual(days, TimeSpan.FromDays(7));
            Assert.AreEqual(weeks, TimeSpan.FromDays(7 * 7));
            Assert.AreEqual(months, TimeSpan.FromDays(7 * 30));
            Assert.AreEqual(years, TimeSpan.FromDays(7 * 365));
        }        
    }
}