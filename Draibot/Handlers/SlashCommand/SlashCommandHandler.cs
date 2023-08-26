using System.Text;
using Discord;
using Discord.WebSocket;
using Draibot.Utils;
using Newtonsoft.Json;

namespace Draibot
{
    public class UserBirthday
    {
        public ulong? GuildID;
        public ulong UserID;
        public string UserMention;
        public string Name;
        public int Day;
        public int Month;
        public int Year;
    }

    internal class SlashCommandHandler
    {
        private DiscordSocketClient discordSocketClient;

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
                default:
                    await command.RespondAsync("Lo siento, parece que el comando ingresado no es válido.");
                    break;
            }
        }

        private async Task GetBirthdays(SocketSlashCommand command)
        {
            StringBuilder birthdaysBuilder = new StringBuilder();
            List<UserBirthday> userBirthdays = JsonUtils.ReadFromJson();
            foreach (UserBirthday userBirthday in userBirthdays)
            {
                if (userBirthday.GuildID == command.GuildId)
                {
                    string day = userBirthday.Day > 9 ? userBirthday.Day.ToString() : $"0{userBirthday.Day}";
                    string month = userBirthday.Month > 9 ? userBirthday.Month.ToString() : $"0{userBirthday.Month}";

                    birthdaysBuilder.Append(
                        $"{userBirthday.Name} - {day}/{month}\n");
                }
            }

            await command.RespondAsync($"Lista de Cumpleaños:\n{birthdaysBuilder}");
        }

        private async Task AddBirthday(SocketSlashCommand command)
        {
            SocketUser birthdayTargetSocketUser = (SocketUser)command.Data.Options.ElementAt(0).Value;
            string birthDate = command.Data.Options.ElementAt(1).Value.ToString()!;
            DateTime parsedDate;
            try
            {
                parsedDate = DateTime.ParseExact(birthDate, "dd/MM/yyyy", null);
            }
            catch (Exception ex)
            {
                throw new Exception("Fecha de cumpleaños inválida.");
            }

            UserBirthday userBirthday = new UserBirthday();
            Console.WriteLine($"{parsedDate.Day} {parsedDate.Month} {parsedDate.Year}");
            userBirthday.GuildID = command.GuildId;
            userBirthday.UserID = birthdayTargetSocketUser.Id;
            userBirthday.UserMention = birthdayTargetSocketUser.Mention;
            userBirthday.Name = birthdayTargetSocketUser.GlobalName;
            userBirthday.Day = parsedDate.Day;
            userBirthday.Month = parsedDate.Month;
            userBirthday.Year = parsedDate.Year;

            JsonUtils.AddBirthdayToBirthdaysJson(userBirthday);

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