using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Reflection;

public class Program
{
    public static Task Main(string[] args) => new Program().MainAsync(args);

    private DiscordSocketClient client;
    private CommandService commandService;
    private LoggingService loggingService;

    public async Task MainAsync(string[] args)
    {

        string? botToken = Environment.GetEnvironmentVariable("bot_token");
        if (string.IsNullOrEmpty(botToken)) throw new Exception($"Argument cannot be null or empty: {nameof(botToken)}");

        client = new DiscordSocketClient();
        commandService = new CommandService();
        loggingService = new LoggingService(client, commandService);

        await SetDiscordActivity();

        // Some alternative options would be to keep your token in an Environment Variable or a standalone file.
        // var token = Environment.GetEnvironmentVariable("NameOfYourEnvironmentVariable");
        // var token = File.ReadAllText("token.txt");
        // var token = JsonConvert.DeserializeObject<AConfigurationClass>(File.ReadAllText("config.json")).Token;

        await RegisterCommandsAsync();

        await client.LoginAsync(TokenType.Bot, botToken);
        await client.StartAsync();

        client.Ready += ClientReady;
        client.SlashCommandExecuted += SlashCommandHandler;
        //client.UserJoined += OnUserJoined;

        // Block this task until the program is closed.
        await Task.Delay(-1);
    }

    private async Task SetDiscordActivity()
    {
        await client.SetActivityAsync(new CustomActivity
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

    private async Task SlashCommandHandler(SocketSlashCommand command)
    {
        // Let's add a switch statement for the command name so we can handle multiple commands in one event.
        switch (command.Data.Name)
        {
            case "list-roles":
                await HandleListRoleCommand(command);
                break;
            case "hora-argentina":
                await HoraArgentina(command);
                break;
        }
    }

    private async Task HoraArgentina(SocketSlashCommand command)
    {
        DateTimeInfo dateTimeInfo = null;

        using (HttpClient client = new HttpClient())
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync($"https://timeapi.io/api/Time/current/zone?timeZone=America/Buenos_Aires");
                response.EnsureSuccessStatusCode(); // Throws an exception if the response is not successful

                string responseBody = await response.Content.ReadAsStringAsync();
                dateTimeInfo = JsonConvert.DeserializeObject<DateTimeInfo>(responseBody);
                Console.WriteLine(responseBody);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        if (dateTimeInfo != null)
        {
            int minutes = dateTimeInfo.Minute;
            string textMinutes = minutes.ToString();
            if(dateTimeInfo.Minute <= 9)
            {
                textMinutes = $"0{dateTimeInfo.Minute}";
            }
            await command.RespondAsync($"La hora en Argentina, donde todo el pais tiene la misma zona horaria es: {dateTimeInfo.Hour}:{textMinutes}:{dateTimeInfo.Seconds}");
        }
        else
            await command.RespondAsync($"Lo siento, no se pudo obtener la hora, consulta con un administrador.");
    }

    private async Task HandleListRoleCommand(SocketSlashCommand command)
    {
        // We need to extract the user parameter from the command. since we only have one option and it's required, we can just use the first option.
        SocketGuildUser guildUser = (SocketGuildUser)command.Data.Options.First().Value;

        // We remove the everyone role and select the mention of each role.
        string roleList = string.Join(",\n", guildUser.Roles.Where(x => !x.IsEveryone).Select(x => x.Mention));

        EmbedBuilder embedBuiler = new EmbedBuilder()
            .WithAuthor(guildUser.ToString(), guildUser.GetAvatarUrl() ?? guildUser.GetDefaultAvatarUrl())
            .WithTitle("Roles")
            .WithDescription(roleList)
            .WithColor(Color.Green)
            .WithCurrentTimestamp();

        // Now, Let's respond with the embed.
        await command.RespondAsync("Esta es una lista de los roles que tienes asignados:", embed: embedBuiler.Build());
    }

    private async Task ClientReady()
    {
        // Let's do our global command

        List<ApplicationCommandProperties> applicationCommandProperties = new List<ApplicationCommandProperties>();
        try
        {
            SlashCommandBuilder listRolesCommand = new SlashCommandBuilder();
            listRolesCommand.WithName("list-roles").WithDescription("Lists all roles of a user.").AddOption("user", ApplicationCommandOptionType.User, "The users whos roles you want to be listed", isRequired: true);
            applicationCommandProperties.Add(listRolesCommand.Build());

            SlashCommandBuilder timeArgentinaCommand = new SlashCommandBuilder();
            timeArgentinaCommand.WithName("hora-argentina").WithDescription("Devuelve la hora actual en Argentina.");
            applicationCommandProperties.Add(timeArgentinaCommand.Build());

            // With global commands we don't need the guild.
            await client.BulkOverwriteGlobalApplicationCommandsAsync(applicationCommandProperties.ToArray());

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

    private async Task CreateGlobalApplicationCommandsManually(SlashCommandBuilder listRolesCommand, SlashCommandBuilder timeArgentinaCommand)
    {
        await client.CreateGlobalApplicationCommandAsync(listRolesCommand.Build());
        await client.CreateGlobalApplicationCommandAsync(timeArgentinaCommand.Build());
    }

    public async Task RegisterCommandsAsync()
    {
        client!.MessageReceived += HandleCommandAsync;
        await commandService!.AddModulesAsync(Assembly.GetEntryAssembly(), null);
    }

    private async Task HandleCommandAsync(SocketMessage arg)
    {
        SocketUserMessage message = arg as SocketUserMessage;
        SocketCommandContext context = new SocketCommandContext(client, message);
        if (message!.Author.IsBot) return;

        int argPos = 0;
        if (message.HasStringPrefix("!", ref argPos))
        {
            IResult result = await commandService.ExecuteAsync(context, argPos, null);
            if (!result.IsSuccess) Console.WriteLine(result.ErrorReason);
            if (result.Error.Equals(CommandError.UnmetPrecondition)) await message.Channel.SendMessageAsync(result.ErrorReason);
        }
    }
}