﻿using System.Diagnostics;
using Discord.Commands;
using Discord.WebSocket;
using System.Reflection;

namespace Draibot
{
    internal class MessageHandler
    {
        private readonly char charPrefix = '!';
        private readonly DiscordSocketClient client;
        private readonly CommandService commandService;

        public MessageHandler(DiscordSocketClient client, CommandService commandService)
        {
            this.commandService = commandService;
            this.client = client;
        }

        public async Task InstallCommandsAsync()
        {
            // Hook the MessageReceived event into our command handler
            client.MessageReceived += HandleCommandAsync;

            // Here we discover all of the command modules in the entry 
            // assembly and load them. Starting from Discord.NET 2.0, a
            // service provider is required to be passed into the
            // module registration method to inject the 
            // required dependencies.
            //
            // If you do not use Dependency Injection, pass null.
            // See Dependency Injection guide for more information.
            await commandService.AddModulesAsync(assembly: Assembly.GetEntryAssembly(),
                services: null);
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            // Don't process the command if it was a system message
            SocketUserMessage? message = messageParam as SocketUserMessage;
            if (message == null) return;

            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;

            // Determine if the message is a command based on the prefix and make sure no bots trigger commands
            if (!(message.HasCharPrefix(charPrefix, ref argPos) ||
                  message.HasMentionPrefix(client.CurrentUser, ref argPos)) ||
                message.Author.IsBot)
                return;

            Console.WriteLine($"[Message Received] Author: {message.Author} | Content: {message.Content}");

            // Create a WebSocket-based command context based on the message
            SocketCommandContext context = new SocketCommandContext(client, message);

            // Execute the command with the command context we just
            // created, along with the service provider for precondition checks.
            await commandService.ExecuteAsync(
                context: context,
                argPos: argPos,
                services: null);
        }
    }
}