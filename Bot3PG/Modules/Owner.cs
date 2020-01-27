using Bot3PG.Data;
using Bot3PG.Handlers;
using Bot3PG.Modules;
using Bot3PG.Modules.General;
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
            string details = "";

            var announceUserJoined = Announce.AnnounceUserJoin(Context.User as SocketGuildUser);
            details += $"{nameof(announceUserJoined)}: {GetResultEmote(!announceUserJoined.IsFaulted)}\n";

            var announceUserLeft = Announce.AnnounceUserLeft(Context.User as SocketGuildUser);
            details += $"{nameof(announceUserLeft)}: {GetResultEmote(!announceUserLeft.IsFaulted)}\n";

            await ReplyAsync(details);
        }
        public IEmote GetResultEmote(bool result) => result ? new Emoji("✅") : new Emoji("❌");

    }
}