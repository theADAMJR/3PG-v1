using Bot3PG.Data;
using Bot3PG.Data.Structs;
using Bot3PG.Handlers;
using Bot3PG.Modules;
using Bot3PG.Modules.General;
using Bot3PG.Modules.Moderation;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Bot3PG.CommandModules
{
    [Color(80, 55, 80)]
    [RequireOwner]
    public sealed class Owner : CommandBase
    {
        internal override string ModuleName => "Owner 🤖";
        internal override Color ModuleColour => Color.LighterGrey;
        
        [Command("GivePro")]
        [Summary("Give Pro to server")]
        public async Task GivePro([Remainder]string role)
        {
            CurrentGuild.IsPremium = true;
            await Guilds.Save(CurrentGuild);
        }

        [Command("GiveRole")]
        [Summary("Give named role to user")]
        public async Task GiveRole([Remainder]string role)
        {
            var socketGuildUser = Context.User as SocketGuildUser;
            var adminRole = Context.Guild.Roles.First(r => r.Name == role);
            await socketGuildUser.AddRoleAsync(adminRole);
        }

        [Command("Update Guilds")]
        [Summary("Update all server documents to the latest version")]
        [RequireOwner]
        public async Task UpdateGuilds()
        {
            int count = 0;
            int failedCount = 0;
            var socketGuilds = Context.Client.Guilds;
            foreach (var socketGuild in socketGuilds)
            {
                try
                {
                    var guild = await Guilds.GetAsync(socketGuild);
                    guild.InitializeModules();
                    await Guilds.Save(guild);
                    count++;           
                }
                catch (Exception) { failedCount++; }
            }
            await ReplyAsync(await EmbedHandler.CreateSimpleEmbed(ModuleName, $":robot: Updated `{count}`/`{count + failedCount}` servers to the latest version!", Color.Purple));
        }

        [Command("Update Users")]
        [Summary("Update all server documents to the latest version")]
        [RequireOwner]
        public async Task UpdateUsers()
        {
            int count = 0;
            int failedCount = 0;
            var allGuildUsers = Context.Client.Guilds.Select(g => g.Users);
            foreach (var guildUsers in allGuildUsers)
            {
                foreach (var guildUser in guildUsers)
                {
                    try
                    {
                        var user = await Users.GetAsync(guildUser as SocketUser);
                        user.Reinitialize();
                        await Users.Save(user);
                        count++;           
                    }
                    catch (Exception) { failedCount++; }
                }
            }
            await ReplyAsync(await EmbedHandler.CreateSimpleEmbed(ModuleName, $":robot: Updated `{count}`/`{count + failedCount}` users!", Color.Purple));
        }

        [Command("Test")]
        public async Task Test()
        {            
            var user = await Users.GetAsync(Context.User as SocketGuildUser);

            string details = "";
            details += TestAnnounce(details);
            // details += TestStaffLogs(details, user);
            details += TestAutoMod(details, user);

            await ReplyAsync(details);
        }

        private string TestAnnounce(string details)
        {
            details += "`Announce`\n";

            var announceUserJoined = Announce.AnnounceUserJoin(Context.User as SocketGuildUser);
            details += $"{nameof(announceUserJoined)}: {GetResultEmote(!announceUserJoined.IsFaulted)}\n";

            var announceUserLeft = Announce.AnnounceUserLeft(Context.User as SocketGuildUser);
            details += $"{nameof(announceUserLeft)}: {GetResultEmote(!announceUserLeft.IsFaulted)}\n";

            return details + "\n";
        }

        private string TestStaffLogs(string details, GuildUser user)
        {
            Punishment createPunishment(PunishmentType punishment) => new Punishment(punishment, "Test", Context.User, DateTime.Now, DateTime.Now);

            details += "`Staff Logs`\n";
            details += Test("Log ban, successful", StaffLogs.LogBan(Context.User, Context.Guild, createPunishment(PunishmentType.Ban)));
            details += Test("Log unban, successful", StaffLogs.LogUnban(Context.User, Context.Guild));
            details += Test("Log mute, successful", StaffLogs.LogMute(user, createPunishment(PunishmentType.Mute)));
            details += Test("Log unmute, successful", StaffLogs.LogUnmute(user, createPunishment(PunishmentType.Mute)));
            details += Test("Log kick, successful", StaffLogs.LogKick(Context.User as SocketGuildUser, createPunishment(PunishmentType.Kick)));
            details += Test("Log messages deleted, null message, successful", StaffLogs.LogMessageDeletion(default, Context.Channel));
            details += Test("Log messages bulk deleted, successful", StaffLogs.LogBulkMessageDeletion(default, Context.Channel));
            return details + "\n";
        }

        public string TestAutoMod(string details, GuildUser user)
        {
            details += "`Auto Mod`\n";
            details += Test("Filter valid message, null returned", Auto.GetContentValidation(CurrentGuild, "", user) == null);
            details += Test("Filter all caps, filter returned", Auto.GetContentValidation(CurrentGuild, "WTF YOU SAY TO ME?!?!?!?", user) == FilterType.AllCaps);
            details += Test("Filter ban links, filter returned", Auto.GetContentValidation(CurrentGuild, ".xxx", user) == FilterType.BadLinks);
            details += Test("Filter bad words, filter returned", Auto.GetContentValidation(CurrentGuild, "ass", user) == FilterType.BadWords);
            details += Test("Filter discord links, filter returned", Auto.GetContentValidation(CurrentGuild, "discord.gg", user) == FilterType.DiscordInvites);
            details += Test("Filter duplicate message, filter returned", Auto.GetContentValidation(CurrentGuild, user.Status.LastMessage, user) == FilterType.DuplicateMessage);
            details += Test("Filter emoji spam, filter returned", Auto.GetContentValidation(CurrentGuild, "😂😂😂😂😂😂😂😂😂", user) == FilterType.EmojiSpam);
            details += Test("Filter mass mention, filter returned", Auto.GetContentValidation(CurrentGuild, "<@!><@!><@!><@!><@!><@!>", user) == FilterType.MassMention);
            details += Test("Filter zalgo, filter returned", Auto.GetContentValidation(CurrentGuild, "Mͭͭͬu̔ͨ͊tͣ̃̚eͨͭ͐ ҉̴̴̢", user) == FilterType.Zalgo);
            return details + "\n";
        }

        public string Test(string label, Task task, bool? result = null) => $"{label}: {GetResultEmote(result ?? !task.IsFaulted)}\n";
        public string Test(string label, bool result) => $"{label}: {GetResultEmote(result)}\n";
        public IEmote GetResultEmote(bool result) => new Emoji(result ? "✅" : "❌");
    }
}