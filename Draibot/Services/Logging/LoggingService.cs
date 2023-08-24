using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Draibot
{
    internal class LoggingService : ILoggingService
    {
        private DiscordSocketClient discordSocketClient;
        private CommandService commandService;

        public LoggingService(DiscordSocketClient client, CommandService command)
        {
            discordSocketClient = client;
            commandService = command;
        }

        public void Initialize()
        {
            discordSocketClient.Log += LogAsync;
            commandService.Log += LogAsync;
        }

        private Task LogAsync(LogMessage message)
        {
            if (message.Exception is CommandException cmdException)
            {
                Console.WriteLine($"[Command/{message.Severity}] {cmdException.Command.Aliases.First()}"
                                  + $" failed to execute in {cmdException.Context.Channel}.");
                Console.WriteLine(cmdException);
            }
            else
                Console.WriteLine($"[General/{message.Severity}] {message}");

            return Task.CompletedTask;
        }
    }
}