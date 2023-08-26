using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace Draibot
{
    public class Program
    {
        private DiscordSocketClient discordSocketClient;
        private CommandService commandService;
        private LoggingService loggingService;
        private SlashCommandHandler slashCommandHandler;
        private MessageHandler _messageHandler;

        private Program()
        {
            discordSocketClient = new DiscordSocketClient();
            commandService = new CommandService();
            slashCommandHandler = new SlashCommandHandler(discordSocketClient);
            _messageHandler = new MessageHandler(discordSocketClient, commandService);
            loggingService = new LoggingService(discordSocketClient, commandService);
        }

        public static async Task Main(string[] args)
        {
            try
            {
                await new Program().RunAsync(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                // Handle the exception or log it.
            }
        }

        private async Task RunAsync(string[] args)
        {
            if (!ValidateBotTokenArgument(args, out string botToken))
            {
                Console.WriteLine("Invalid bot token argument.");
                return;
            }

            await InitializeDependenciesAsync(botToken);

            discordSocketClient.Ready += DiscordSocketClientReady;
            //client.UserJoined += OnUserJoined;

            // Block until cancellation is requested.
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };
            await Task.Delay(-1, cts.Token);
        }

        private async Task InitializeDependenciesAsync(string botToken)
        {
            slashCommandHandler.Initialize();
            loggingService.Initialize();
            await _messageHandler.InstallCommandsAsync();
            await InitializeBotAsync(botToken);
        }

        private bool ValidateBotTokenArgument(string[] args, out string botToken)
        {
            botToken = null;
            if (args.Length < 1 || string.IsNullOrEmpty(args[0]))
            {
                Console.WriteLine("Argument cannot be null or empty: bot_token");
                return false;
            }

            botToken = args[0];
            return true;
        }

        private async Task InitializeBotAsync(string botToken)
        {
            await SetDiscordActivityAsync();
            await discordSocketClient.LoginAsync(TokenType.Bot, botToken);
            await discordSocketClient.StartAsync();
        }

        private async Task SetDiscordActivityAsync()
        {
            await discordSocketClient.SetActivityAsync(new CustomActivity
            {
                Name = "Roblox",
                Type = ActivityType.Competing,
            });
        }

        /*
        private async Task OnUserJoined(SocketGuildUser user)
        {
            ulong welcomeChannelId = 1133166132803666021; // Replace with the ID of your welcome channel
            var welcomeChannel = client.GetChannel(welcomeChannelId) as SocketTextChannel;
    
            string welcomeMessage = $"Welcome, {user.Mention}, to our server! Feel free to introduce yourself.";
            await welcomeChannel.SendMessageAsync(welcomeMessage);
        }
        */

        private async Task DiscordSocketClientReady()
        {
            List<ApplicationCommandProperties> applicationCommandProperties = new List<ApplicationCommandProperties>();
            try
            {
                SlashCommandBuilder listRolesCommand = new SlashCommandBuilder();
                listRolesCommand.WithName("list-roles").WithDescription("Lists all roles of a user.").AddOption("user",
                    ApplicationCommandOptionType.User, "The users whos roles you want to be listed", isRequired: true);
                applicationCommandProperties.Add(listRolesCommand.Build());

                SlashCommandBuilder timeArgentinaCommand = new SlashCommandBuilder();
                timeArgentinaCommand.WithName("hora-argentina")
                    .WithDescription("Devuelve la hora actual en Argentina.");
                applicationCommandProperties.Add(timeArgentinaCommand.Build());

                // TODO: Refactor/remove from here.
                List<SlashCommandOptionBuilder> addBirthdayCommandOptions = new List<SlashCommandOptionBuilder>();
                addBirthdayCommandOptions.Add(new SlashCommandOptionBuilder()
                {
                    Name = "Nombre",
                    Type = ApplicationCommandOptionType.String,
                    Description = "El nombre del cumpleañero.",
                    IsRequired = true,
                    MaxLength = 50,
                });
                addBirthdayCommandOptions.Add(new SlashCommandOptionBuilder()
                {
                    Name = "Fecha",
                    Type = ApplicationCommandOptionType.String,
                    Description = "La fecha de cumpleaños en formato DD/MM/YYYY",
                    IsRequired = true,
                    MaxLength = 10,
                });
                SlashCommandBuilder addBirthdayCommand = new SlashCommandBuilder();
                addBirthdayCommand.WithName("agregar-cumpleaños")
                    .WithDescription("Registra un cumpleaños.")
                    .AddOptions(addBirthdayCommandOptions.ToArray());
                applicationCommandProperties.Add(addBirthdayCommand.Build());

                SlashCommandBuilder getBirthdays = new SlashCommandBuilder();
                getBirthdays.WithName("listar-cumpleaños")
                    .WithDescription("Muestra la lista de cumpleaños del servidor.");
                applicationCommandProperties.Add(getBirthdays.Build());

                // With global commands we don't need the guild.
                await discordSocketClient.BulkOverwriteGlobalApplicationCommandsAsync(applicationCommandProperties
                    .ToArray());

                // TODO: [REFACTOR THIS FUNCTION, MAYBE MOVE TO IT'S OWN FILE.]
                // Using the ready event is a simple implementation for the sake of the example. Suitable for testing and development.
                // For a production bot, it is recommended to only run the CreateGlobalApplicationCommandAsync() once for each command.
                //await CreateGlobalApplicationCommandsManually(listRolesCommand, timeArgentinaCommand);
            }
            catch (HttpException exception)
            {
                // If our command was invalid, we should catch an ApplicationCommandException. This exception contains the path of the error as well as the error message. You can serialize the Error field in the exception to get a visual of where your error is.
                string json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);

                // You can send this error somewhere or just print it to the console, for this example we're just going to print it.
                Console.WriteLine(json);
            }
        }

        private async Task CreateGlobalApplicationCommandsManually(SlashCommandBuilder listRolesCommand,
            SlashCommandBuilder timeArgentinaCommand)
        {
            await discordSocketClient.CreateGlobalApplicationCommandAsync(listRolesCommand.Build());
            await discordSocketClient.CreateGlobalApplicationCommandAsync(timeArgentinaCommand.Build());
        }
    }
}