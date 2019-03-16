using Bot3PG.Core.LevelingSystem;
using Bot3PG.Core.Users;
using Bot3PG.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Victoria;

namespace Bot3PG.Handlers
{
    public class CommandHandler
    {
        private readonly CommandService commands;
        private readonly IServiceProvider services;

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
        }
        
        public void HookEvents()
        {
            Global.Client.MessageReceived += HandleCommandAsync;
        }
        
        private async Task HandleCommandAsync(SocketMessage socketMessage)
        {
            var argPos = 0;
            if (!(socketMessage is SocketUserMessage message) || message.Author.IsBot || message.Author.IsWebhook || message.Channel is IPrivateChannel)
                return;
            
            if (!message.HasStringPrefix(Global.Config.CommandPrefix, ref argPos))
            {
                Leveling.ValidateMessageForXP(socketMessage as SocketUserMessage);
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
                await context.Channel.SendMessageAsync("", embed: await EmbedHandler.CreateBasicEmbed("Error", $"{result.Result.ErrorReason}", Color.Red));
            }
        }
    }
}