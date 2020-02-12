using System.Threading.Tasks;
using Bot3PG.Data;
using Bot3PG.Data.Structs;
using Bot3PG.Modules.Moderation;
using NUnit.Framework;
using Moq;
using Discord.WebSocket;
using Discord;
using System;
using System.Linq;

namespace Bot3PG.Tests
{
    public class AutoTests : TestModule
    {
        private IGuildUser socketGuildUser;
        private GuildUser guildUser;
        private Guild guild;

        [SetUp]
        public async Task Initialize()
        {
            socketGuildUser = CreateMockGuildUser();
            guildUser = new GuildUser(socketGuildUser);
            guild = new Guild(socketGuildUser.Guild);

            var autoMod = guild.Moderation.Auto;
            foreach (FilterType filter in Enum.GetValues(typeof(FilterType)))
                autoMod.Filters = autoMod.Filters.Append(new FilterProperties { Filter = filter }).ToArray();

            autoMod.UseDefaultBanWords = true;
            autoMod.UseDefaultBanLinks = true;
        }

        [Test]
        public void ValidateMessage_FamilyFriendly_NullReturned()
        {
            var filter = Auto.GetContentValidation(guild, "Ayup", guildUser);

            Assert.IsNull(filter);
        }

        [Test]
        public void ValidateMessage_TriggerMessages_FiltersReturned()
        {
            System.Console.WriteLine(guild.Moderation.Auto.Filters.Count());

            var filter = Auto.GetContentValidation(guild, "Ayup", guildUser);
            
            var badWord = Auto.GetContentValidation(guild, "ass", guildUser);
            var badLink = Auto.GetContentValidation(guild, "shrek.xxx", guildUser);
            var allCaps = Auto.GetContentValidation(guild, "SHREK 5 IS GREATEST MOVIE!!!", guildUser);
            var discordInvites = Auto.GetContentValidation(guild, "discord.gg/shrek", guildUser);
            var duplicateMessage = Auto.GetContentValidation(guild, "", guildUser);
            var emojiSpam = Auto.GetContentValidation(guild, "🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔", guildUser);
            var massMention = Auto.GetContentValidation(guild, "<@!><@!><@!><@!><@!><@!>", guildUser);
            var zalgo = Auto.GetContentValidation(guild, "Mͭͭͬu̔ͨ͊tͣ̃̚eͨͭ͐ ҉̴̴̢dͫ͒ͯ", guildUser);

            Assert.IsNull(filter);
            Assert.AreEqual(FilterType.BadWords, badWord);
            Assert.AreEqual(FilterType.BadLinks, badLink);
            Assert.AreEqual(FilterType.AllCaps, allCaps);
            Assert.AreEqual(FilterType.DiscordInvites, discordInvites);
            // Assert.AreEqual(FilterType.DuplicateMessage, duplicateMessage);
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