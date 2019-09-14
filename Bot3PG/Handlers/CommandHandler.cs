using Bot3PG.Modules;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Reflection;
using Bot3PG.Core.Data;
using Bot3PG.Modules.XP;
using Bot3PG.Modules.General;
using System.Collections.Generic;
using System.Linq;

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

            HookEvents();
        }
        
        public async Task InitializeAsync()
        {
            await commands.AddModulesAsync(
                assembly: Assembly.GetEntryAssembly(),
                services: services);
            commandHelp = new CommandHelp();
        }

        public void HookEvents() => Global.Client.MessageReceived += HandleCommandAsync;

        private async Task HandleCommandAsync(SocketMessage socketMessage)
        {
            if (!(socketMessage is SocketUserMessage message) || message.Author.IsBot || message.Author.IsWebhook || message.Channel is IPrivateChannel) return;

            var guild = await Guilds.GetAsync((socketMessage.Author as SocketGuildUser).Guild);
            var commandPrefix = guild.General.CommandPrefix ?? "/";

            int argPos = 0;
            if (!message.HasStringPrefix(commandPrefix, ref argPos))
            {
                LevelingSystem.ValidateForXPAsync(socketMessage as SocketUserMessage);
                return;
            }

            var context = new SocketCommandContext(Global.Client, socketMessage as SocketUserMessage);

            /* Check if the channel ID that the message was sent from is in our Config - Blacklisted Channels.
            var blacklistedChannelCheck = from a in Config.bot.BlacklistedChannels
                                          where a == context.Channel.Id
                                          select a;
            var blacklistedChannel = blacklistedChannelCheck.FirstOrDefault();

            If the Channel ID is in the list of blacklisted channels. Ignore the command.
            if (blacklistedChannel == context.Channel.Id)
            {
                return Task.CompletedTask;
            } */
            //else

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
                        await context.Channel.SendMessageAsync("", embed: await EmbedHandler.CreateErrorEmbed("Insufficient permissions", $"**Required permissions:** "));
                        break;
                    default: // if in debug mode
                        await context.Channel.SendMessageAsync("", embed: await EmbedHandler.CreateErrorEmbed("Error", $"{result.Exception.Message} \n**Source**: {result.Exception.StackTrace}"));
                        break;
                }
            }
        }

        private string CorrectCommandUsage(SocketUserMessage message, string prefix)
        {
            foreach (var command in commandHelp)
            {
                if (message.Content.ToLower().Contains(command.Key))
                {
                    return "`" + prefix + commandHelp[command.Key].Usage + "`";
                }
            }
            return null;
        }
    }
}