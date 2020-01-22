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