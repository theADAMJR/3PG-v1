using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Reflection;
using Bot3PG.Data;
using Bot3PG.Modules.XP;
using Bot3PG.Modules.General;
using System.Linq;
using System.Collections.Generic;
using Bot3PG.Modules;
using Bot3PG.Data.Structs;

namespace Bot3PG.Handlers
{
    public class CommandHandler
    {
        private readonly CommandService commands;
        private readonly IServiceProvider services;
        private CommandHelp commandHelp;

        public CommandHandler(IServiceProvider services)
        {
            commands = services.GetRequiredService<CommandService>();
            this.services = services;
        }
        
        public async Task InitializeAsync()
        {
            await commands.AddModulesAsync(
                assembly: Assembly.GetEntryAssembly(),
                services: services);
            commandHelp = new CommandHelp();
        }

        public async Task HandleCommandAsync(SocketMessage socketMessage)
        {
            System.Console.WriteLine(socketMessage.Content);
            if (!(socketMessage is SocketUserMessage message)
                || message.Author.IsWebhook
                || message.Channel is IPrivateChannel) return;
            if (!(socketMessage.Author is SocketGuildUser guildAuthor)) return;

            Guild guild = null;
            try { guild = await Guilds.GetAsync(guildAuthor.Guild); }
            catch
            {
                await message.Channel.SendMessageAsync(embed:
                    await EmbedHandler.CreateErrorEmbed("Database",
                        "Server configuration corrupted. Please type /reset to reset it."));
                return;
            }
            var prefix = guild?.General.CommandPrefix ?? "/";

            int position = 0;
            bool isCommand = message.HasStringPrefix(prefix, ref position);

            bool userCanEarnEXP = guild.XP.Enabled && !message.Author.IsBot;
            if (!isCommand && userCanEarnEXP)
            {
                try { await Leveling.ValidateForEXPAsync(message, guild); }
                catch {}
                return;
            }
            if (message.Author.IsBot) return;

            var context = new CustomCommandContext(Global.Client, message) { CurrentGuild = guild };

            CommandValidation validation;
            try { validation = ValidateCommandExists(position, context); }
            catch (ArgumentNullException) { return; }
            var command = validation.Command;

            try { ValidateCommand(command, guild, message); }
            catch (InvalidOperationException ex)
            {
                await message.Channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Commands", ex.Message));
                return;
            }

            var execution = commands.ExecuteAsync(context, position, services, MultiMatchHandling.Best);
            if (!execution.Result.IsSuccess)
                await HandleFailedExecution(message, prefix, execution);
        }

        private CommandValidation ValidateCommandExists(int position, SocketCommandContext context)
        {
            var searchResult = commands.Search(context, position);
            var command = searchResult.Commands.FirstOrDefault().Command;
            return new CommandValidation { Search = searchResult, Command = command };
        }

        private async Task HandleFailedExecution(SocketUserMessage message, string prefix, Task<IResult> execution)
        {
            switch (execution.Result.Error)
            {
                case CommandError.BadArgCount:
                    await message.Channel.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed("❌ Incorrect usage", $"**Correct usage:** {CorrectCommandUsage(message, prefix)}", Color.Red));
                    break;
                case CommandError.Exception:
                    await message.Channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("🤔 Something went wrong", $"{execution.Result.ErrorReason}"));
                    break;
                case CommandError.ParseFailed:
                    await message.Channel.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed("🚫 Invalid arguments", $"**Correct usage:** {CorrectCommandUsage(message, prefix)}", Color.Red));
                    break;
                case CommandError.ObjectNotFound:
                    await message.Channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("👀 Not found", $"{execution.Result.ErrorReason}"));
                    break;
                case CommandError.UnmetPrecondition:
                    await message.Channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("🔒 Insufficient permissions", $"**Required permissions:** {RequiredPermissions(message)}"));
                    break;
                default:
                    await message.Channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Error", $"{execution.Exception.Message} \n**Source**: {execution.Exception.StackTrace}"));
                    break;
            }
        }

        protected void ValidateCommand(CommandInfo command, Guild guild, SocketUserMessage message)
        {
            var channelIsBlacklisted = guild.General.BlacklistedChannels.Any(id => id == message.Channel.Id);
            if (channelIsBlacklisted)
                throw new InvalidOperationException("Channel is blacklisted.");

            var module = guild.GetType().GetProperty(command.Module.Name)?.GetValue(guild) as CommandConfigModule;
            if (module is null) return;
            else if (!module.Enabled) 
                throw new InvalidOperationException("Module is not enabled.");

            var commandOverride = module.Commands.Overrides.FirstOrDefault(c => c?.Name?.ToLower() == command?.Name?.ToLower());
            if (commandOverride != null && !commandOverride.Enabled)
                throw new InvalidOperationException("Command is disabled.");
            
            var guildAuthor = message.Author as SocketGuildUser;
            bool isAuthorized = commandOverride is null || guildAuthor.GuildPermissions.Has(commandOverride.Permission);
            if (!isAuthorized) 
                throw new InvalidOperationException("Insufficient permissions.");
        }

        private string CorrectCommandUsage(SocketUserMessage message, string prefix)
        {
            string content = message.Content.ToLower();
            string similarCommand = commandHelp.FirstOrDefault(c => content.Contains(c.Key)).Key;
            
            string usedAlias = null;
            foreach (var command in commandHelp)
                foreach (var alias in command.Value.Alias)
                    if (message.Content.Split(" ")[0].Contains(alias)) 
                        usedAlias = alias;

            var discordCommand = Global.CommandService.Commands.FirstOrDefault(c => c.Name.ToLower() == similarCommand || c.Aliases.Contains(usedAlias));
            return discordCommand != null ? $"`{prefix}{CommandHelp.GetUsage(discordCommand, similarCommand ?? usedAlias)}`" : null;
        }

        private string RequiredPermissions(SocketUserMessage message)
        {
            foreach (var command in commandHelp)
            {
                if (message.Content.ToLower().Contains(command.Key))
                {
                    var preconditions = new List<string>();
                    foreach (var precondition in commandHelp[command.Key].Preconditions)
                        preconditions.Add(precondition.ToString());
                    return $"`{preconditions[0]}`";
                } 
            }
            return null;
        }
    }
}