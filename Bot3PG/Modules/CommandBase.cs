using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Bot3PG.Modules
{
    public class CommandBase : ModuleBase<SocketCommandContext>
    {
        public async Task<IUserMessage> ReplyAsync(EmbedBuilder embed)
        {
            if (!embed.Color.HasValue)
            {
                embed.WithColor(74, 48, 80);
            }
            return await base.ReplyAsync("", embed: embed.Build());
        }
        public async Task<IUserMessage> ReplyAsync(Task<Embed> embed) => await ReplyAsync(await embed);
        public async Task<IUserMessage> ReplyAsync(Embed embed) => await base.ReplyAsync("", embed: embed);
        public async Task<IUserMessage> ReplyAsync(string message) => await base.ReplyAsync(message);

        public async Task<IUserMessage> ReplyToUserAsync(SocketUser target, EmbedBuilder embed)
        {
            if (!embed.Color.HasValue)
            {
                embed.WithColor(74, 48, 80);
            }
            return await target.SendMessageAsync("", embed: embed.Build());
        }
        public async Task<IUserMessage> ReplyToUserAsync(SocketUser target, Task<Embed> embed) => await ReplyToUserAsync(target, await embed);
        public async Task<IUserMessage> ReplyToUserAsync(SocketUser target, Embed embed) => await target.SendMessageAsync("", embed: embed);
        public async Task<IUserMessage> ReplyToUserAsync(SocketUser target, string message) => await target.SendMessageAsync(message);
    }
}