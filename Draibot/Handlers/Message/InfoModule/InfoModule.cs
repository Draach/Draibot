using Discord.Commands;

namespace Draibot
{
    /// <summary>
    /// Example base message command for future references mostly.
    /// </summary>
    public class InfoModule : ModuleBase<SocketCommandContext>
    {
        private string helpMessage = @"
            Aquí tienes una lista de comandos disponibles para Draibot:
            /hora-argentina - Indica la hora en argentina.";

        [Command("help")]
        [Summary("Retrieve a list of commands and their descriptions.")]
        public Task SayAsync()
            => ReplyAsync(helpMessage);
    }
}