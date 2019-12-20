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
            if (!(socketMessage is SocketUserMessage message) || message.Author.IsBot || message.Author.IsWebhook || message.Channel is IPrivateChannel) return;

            var socketGuildUser = socketMessage.Author as SocketGuildUser;
            if (socketGuildUser is null) return;

            var guild = new Guild(socketGuildUser.Guild);
            try { guild = await Guilds.GetAsync(socketGuildUser.Guild); }
            catch
            {
                await socketMessage.Channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Database", "Server configuration corrupted. Please type /reset to reset it."));
                return;
            }
            var commandPrefix = guild?.General?.CommandPrefix ?? "/";

            int argPos = 0;
            if (!message.HasStringPrefix(commandPrefix, ref argPos))
            {
                Leveling.ValidateForXPAsync(socketMessage as SocketUserMessage);
                return;
            }

            var context = new SocketCommandContext(Global.Client, socketMessage as SocketUserMessage);

            var channelIsBlacklisted = guild.General.BlacklistedChannels.Any(id => id == message.Channel.Id);
            if (channelIsBlacklisted) return;

            var result = commands.ExecuteAsync(context, argPos, services, MultiMatchHandling.Best);

            if (!result.Result.IsSuccess)
            {
                switch (result.Result.Error)
                {
                    case CommandError.BadArgCount:
                        await context.Channel.SendMessageAsync("", embed: await EmbedHandler.CreateBasicEmbed("Incorrect usage", $"**Correct usage:** {CorrectCommandUsage(message, commandPrefix)}", Color.Red));
                        break;
                    case CommandError.Exception:
                        await context.Channel.SendMessageAsync("", embed: await EmbedHandler.CreateErrorEmbed("Something went wrong", $"{result.Result.ErrorReason}"));
                        break;
                    case CommandError.ParseFailed:
                        await context.Channel.SendMessageAsync("", embed: await EmbedHandler.CreateBasicEmbed("Invalid arguments", $"**Correct usage:** {CorrectCommandUsage(message, commandPrefix)}", Color.Red));
                        break;
                    case CommandError.UnknownCommand:
                        var errorMessage = CorrectCommandUsage(message, commandPrefix) != null ? 
                            $"**Did you mean** " + CorrectCommandUsage(message, commandPrefix) + "?" : $"No similar commands found. Type `{commandPrefix}help` for a list of commands.";
                        await context.Channel.SendMessageAsync("", embed: await EmbedHandler.CreateBasicEmbed("Unknown command", errorMessage, Color.Red));
                        break;
                    case CommandError.ObjectNotFound:
                        await context.Channel.SendMessageAsync("", embed: await EmbedHandler.CreateErrorEmbed("⚠💀 Extreme Error!", $"{result.Result.ErrorReason}"));
                        break;
                    case CommandError.UnmetPrecondition:
                        await context.Channel.SendMessageAsync("", embed: await EmbedHandler.CreateErrorEmbed("Insufficient permissions", 
                            $"**Required permissions:** {RequiredPermissions(message)}"));
                        break;
                    default: // TODO - if in debug mode
                        await context.Channel.SendMessageAsync("", embed: await EmbedHandler.CreateErrorEmbed("Error", $"{result.Exception.Message} \n**Source**: {result.Exception.StackTrace}"));
                        break;
                }
            }
        }

        private string CorrectCommandUsage(SocketUserMessage message, string prefix)
        {
            var similarCommand = commandHelp.FirstOrDefault(c => message.Content.ToLower().Contains(c.Key)).Key;
            return similarCommand != null ? $"`{prefix}{commandHelp[similarCommand].Usage}`" : null;
        }

        private string RequiredPermissions(SocketUserMessage message)
        {
            foreach (var command in commandHelp)
            {
                if (message.Content.ToLower().Contains(command.Key))
                {
                    var preconditions = new List<string>();
                    foreach (var precondition in commandHelp[command.Key].Preconditions)
                    {
                        preconditions.Add(precondition.ToString());
                    }
                    return $"`{preconditions[0]}`";
                } 
            }
            return null;
        }
    }
}