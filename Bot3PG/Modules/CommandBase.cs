using System;
using System.Linq;
using System.Threading.Tasks;
using Bot3PG.Data;
using Bot3PG.Data.Structs;
using Bot3PG.Handlers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Bot3PG.Modules
{
    public abstract class CommandBase : ModuleBase<SocketCommandContext>
    {
        internal Guild CurrentGuild { get; set; }

        internal abstract string ModuleName { get; }
        internal abstract Color ModuleColour { get; }

        protected async override void BeforeExecute(CommandInfo command)
        {
            CurrentGuild = await Guilds.GetAsync(Context.Guild);
            
            // var module = CurrentGuild.GetType().GetProperty(command.Module.Name).GetValue(CurrentGuild) as CommandConfigModule;            
            // if (module is null || !module.Enabled) throw new CommandException(command, Context, new CommandDisabledException("Module is not enabled."));

            // if command name or alias found by command override
            //var commandOverride = module.Commands.FirstOrDefault(c => c.Name.ToLower() == command.Name.ToLower());
            //if (!commandOverride?.Enabled ?? false) throw new CommandException(command, Context, new CommandDisabledException("Command is disabled."));
            
            // var socketGuildUser = Context.User as SocketGuildUser;
            //bool isAuthorized = commandOverride is null || socketGuildUser.GuildPermissions.Has(commandOverride.Permission);
            //if (!isAuthorized) throw new CommandException(command, Context, new CommandDisabledException("Insufficient permissions."));
        }

        protected async override void AfterExecute(CommandInfo command)
        {
            CurrentGuild ??= await Guilds.GetAsync(Context.Guild);
            if (CurrentGuild.General.RemoveCommandMessages)
            {
                try { await Context.Message.DeleteAsync(); }
                catch (System.Exception) {}
            } 
        }
        
        public async Task<IUserMessage> ReplyAsync(EmbedBuilder embed)
        {
            if (!embed.Color.HasValue)
            {
                embed.WithColor(74, 48, 80);
            }
            return await base.ReplyAsync(embed: embed.Build());
        }
        public async Task<IUserMessage> ReplyAsync(Task<Embed> embed) => await ReplyAsync(await embed);
        public async Task<IUserMessage> ReplyAsync(Embed embed) => await base.ReplyAsync(embed: embed);
        public async Task<IUserMessage> ReplyAsync(string message) => await base.ReplyAsync(message);

        public async Task<IUserMessage> ReplyToUserAsync(SocketUser target, EmbedBuilder embed)
        {
            if (!embed.Color.HasValue)
            {
                embed.WithColor(74, 48, 80);
            }
            return await target.SendMessageAsync(embed: embed.Build());
        }
        public async Task<IUserMessage> ReplyToUserAsync(SocketUser target, Task<Embed> embed) => await ReplyToUserAsync(target, await embed);
        public async Task<IUserMessage> ReplyToUserAsync(SocketUser target, Embed embed) => await target.SendMessageAsync(embed: embed);
        public async Task<IUserMessage> ReplyToUserAsync(SocketUser target, string message) => await target.SendMessageAsync(message);
    }
}