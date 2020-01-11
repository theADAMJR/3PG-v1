using Bot3PG.Data;
using Bot3PG.Handlers;
using Bot3PG.Modules;
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

        [Command("Advertise")]
        [Summary("Send messages to predefined list of people")]
        [RequireOwner]
        public async Task Advertise()
        {
            ulong[] userIds = { 218459216145285121, 308584056944328705 };
            var users = userIds.Select(id => Context.Client.GetUser(id));
            int count = 0;
            
            foreach (var user in users)
            {
                try
                {
                    if (user.IsBot) continue;

                    await user.SendMessageAsync($"Hey {user.Username},\nDo you want a bot with Music Commands, Twitch Alerts, Staff Logs, \n" +
                    "EXP, Ban panels, Web dashboard, Auto-moderation, Ruleboxes… and much more? https://3pg.xyz\n" +
                    "(if not, please ignore this message, and sorry for messaging you)");                    
                    count++;
                }
                catch {}
            }
            await ReplyAsync(EmbedHandler.CreateSimpleEmbed(ModuleName, $"Message sent to {count} users", Color.Green));
        }
        
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

        [Command("Test")]
        [Summary("Create a test embed with reactions")]
        public async Task Test([Remainder] string details)
        {
            var features = details.Split("|");
            const int maxLength = 2;
            if (features.Length <= 1 || features.Length > maxLength)
            {
                await ReplyAsync(EmbedHandler.CreateErrorEmbed(ModuleName, "Please separate Title and Expected with | (vertical bar)"));
                return;
            }
            await Context.Message.DeleteAsync();

            var embed = new EmbedBuilder()
                .WithTitle($"`TEST` {features[0]}")
                .WithDescription(features[1])
                .WithColor(ModuleColour);
            
            var message = await ReplyAsync(embed);
            await message.AddReactionsAsync(new []{ new Emoji("✅"), new Emoji("❌")});
        }

        [Command("Update")]
        [Summary("Update all server documents to the latest version")]
        [RequireOwner]
        public async Task Update()
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

    }
}