using Bot3PG.Data.Structs;
using Bot3PG.Modules.Moderation;
using NUnit.Framework;
using Moq;
using System;
using System.Linq;

namespace Bot3PG.Tests
{
    public class AutoTests : TestModule
    {
        [SetUp]
        public void Setup()
        {
            var autoMod = Guild.Moderation.Auto;
            foreach (FilterType filter in Enum.GetValues(typeof(FilterType)))
                autoMod.Filters = autoMod.Filters.Append(new FilterProperties { Filter = filter }).ToArray();

            autoMod.UseDefaultBanWords = true;
            autoMod.UseDefaultBanLinks = true;

            GuildUser.Status.LastMessage = "test";
        }

        [Test]
        public void ValidateMessage_FamilyFriendly_NullReturned()
        {
            var filter = Auto.GetContentValidation(Guild, "Ayup", GuildUser);

            Assert.IsNull(filter);
        }

        [Test]
        public void ValidateMessage_TriggerMessages_FiltersReturned()
        {
            var filter = Auto.GetContentValidation(Guild, "Ayup", GuildUser);
            var badWord = Auto.GetContentValidation(Guild, "ass", GuildUser);
            var badLink = Auto.GetContentValidation(Guild, "shrek.xxx", GuildUser);
            var allCaps = Auto.GetContentValidation(Guild, "SHREK 5 IS GREATEST MOVIE!!!", GuildUser);
            var discordInvites = Auto.GetContentValidation(Guild, "discord.gg/shrek", GuildUser);
            var duplicateMessage = Auto.GetContentValidation(Guild, "test", GuildUser);
            var emojiSpam = Auto.GetContentValidation(Guild, "🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔", GuildUser);
            var massMention = Auto.GetContentValidation(Guild, "<@!><@!><@!><@!><@!><@!>", GuildUser);
            var zalgo = Auto.GetContentValidation(Guild, "Mͭͭͬu̔ͨ͊tͣ̃̚eͨͭ͐ ҉̴̴̢dͫ͒ͯ", GuildUser);

            Assert.IsNull(filter);
            Assert.AreEqual(FilterType.BadWords, badWord);
            Assert.AreEqual(FilterType.BadLinks, badLink);
            Assert.AreEqual(FilterType.AllCaps, allCaps);
            Assert.AreEqual(FilterType.DiscordInvites, discordInvites);
            Assert.AreEqual(FilterType.DuplicateMessage, duplicateMessage);
            Assert.AreEqual(FilterType.EmojiSpam, emojiSpam);
            Assert.AreEqual(FilterType.MassMention, massMention);
            Assert.AreEqual(FilterType.Zalgo, zalgo);
        }
    }

    public class StaffLogsTests
    {
        public StaffLogsTests()
        {
            
        }
    }
}