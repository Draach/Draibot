using System.Text;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

namespace Draibot
{
    internal class UserBirthDate
    {
        public string Name;
        public int Day;
        public int Month;
        public int Year;
    }

    internal class SlashCommandHandler
    {
        private DiscordSocketClient discordSocketClient;
        private List<UserBirthDate> birthDates = new();

        public SlashCommandHandler(DiscordSocketClient discordSocketClient)
        {
            this.discordSocketClient = discordSocketClient;
        }

        public void Initialize()
        {
            discordSocketClient.SlashCommandExecuted += HandleSlashCommands;
        }

        private async Task HandleSlashCommands(SocketSlashCommand command)
        {
            // Let's add a switch statement for the command name so we can handle multiple commands in one event.
            switch (command.Data.Name)
            {
                case "list-roles":
                    await HandleListRoleCommand(command);
                    break;
                case "hora-argentina":
                    await TimeArgentina(command);
                    break;
                case "agregar-cumpleaños":
                    await AddBirthday(command);
                    break;
                case "listar-cumpleaños":
                    await GetBirthdays(command);
                    break;
            }
        }

        private async Task GetBirthdays(SocketSlashCommand command)
        {
            StringBuilder birthdaysBuilder = new StringBuilder();
            foreach (UserBirthDate userBirthDate in birthDates)
            {
                birthdaysBuilder.Append(
                    $"{userBirthDate.Name} - {userBirthDate.Day}/{userBirthDate.Month}/{userBirthDate.Year}\n");
            }

            await command.RespondAsync($"Lista de Cumpleaños:\n{birthdaysBuilder}");
        }

        private async Task AddBirthday(SocketSlashCommand command)
        {
            Console.WriteLine($"GuildId: {command.GuildId} | ChannelId: {command.ChannelId}.");
            Console.WriteLine($"Command name: {command.Data.Name}");
            Console.WriteLine($"Options:");
            foreach (SocketSlashCommandDataOption socketSlashCommandDataOption in command.Data.Options)
            {
                Console.WriteLine(
                    $"Option name: {socketSlashCommandDataOption.Name} | value: {socketSlashCommandDataOption.Value}");
            }

            string birthDate = command.Data.Options.ElementAt(1).Value.ToString()!;
            DateTime parsedDate = DateTime.ParseExact(birthDate, "dd/MM/yyyy", null);
            // By now we store them in memory, replace with database later.
            UserBirthDate userBirthDate = new UserBirthDate();
            Console.WriteLine($"{parsedDate.Day} {parsedDate.Month} {parsedDate.Year}");
            userBirthDate.Name = command.Data.Options.ElementAt(0).Value.ToString()!;
            userBirthDate.Day = parsedDate.Day;
            userBirthDate.Month = parsedDate.Month;
            userBirthDate.Year = parsedDate.Year;

            birthDates.Add(userBirthDate);
            
            await command.RespondAsync($"Cumpleaños registrado con éxito!");
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
            await command.RespondAsync("Esta es una lista de los roles que tienes asignados:",
                embed: embedBuiler.Build());
        }

        private async Task TimeArgentina(SocketSlashCommand command)
        {
            DateTimeInfo dateTimeInfo = null;

            using (HttpClient httpClient = new HttpClient())
            {
                try
                {
                    // TODO: Move to it's own HttpModule or HttpService.
                    HttpResponseMessage response =
                        await httpClient.GetAsync(
                            $"https://timeapi.io/api/Time/current/zone?timeZone=America/Buenos_Aires");
                    response.EnsureSuccessStatusCode(); // Throws an exception if the response is not successful

                    string responseBody = await response.Content.ReadAsStringAsync();
                    dateTimeInfo = JsonConvert.DeserializeObject<DateTimeInfo>(responseBody);
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
                if (dateTimeInfo.Minute <= 9)
                {
                    textMinutes = $"0{dateTimeInfo.Minute}";
                }

                await command.RespondAsync(
                    $"La hora en Argentina, donde todo el pais tiene la misma zona horaria es: {dateTimeInfo.Hour}:{textMinutes}:{dateTimeInfo.Seconds}");
            }
            else
                await command.RespondAsync($"Lo siento, no se pudo obtener la hora, consulta con un administrador.");
        }
    }
}