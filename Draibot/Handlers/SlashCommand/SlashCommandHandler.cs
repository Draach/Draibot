using System.Security.Cryptography;
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
        public DateTime BirthDate;
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
                case "roll":
                    await Roll(command);
                    break;
                default:
                    await command.RespondAsync("Lo siento, parece que el comando ingresado no es válido.");
                    break;
            }
        }

        private async Task Roll(SocketSlashCommand command)
        {
            if (int.TryParse(command.Data.Options.ElementAt(0).Value.ToString(), out int faces))
            {
                int result = RollDice(faces);
                await command.RespondAsync($"{command.User.Mention} ha obtenido un {result} en su tirada!");
            }
            else
            {
                throw new Exception();
            }
        }

        public int RollDice(int maxValue)
        {
            Random random = new Random();
            if (maxValue < 1)
            {
                throw new ArgumentException("The maximum value must be at least 1.");
            }


            int result = random.Next(1, maxValue);

            return result;
        }

        private async Task GetBirthdays(SocketSlashCommand command)
        {
            StringBuilder birthdaysBuilder = new StringBuilder();
            SortedDictionary<DateTime, List<UserBirthday>> userBirthdays = JsonUtils.ReadFromJson();
            var sortedBirthdays = userBirthdays.OrderBy(pair => new DateTime(1, pair.Key.Month, pair.Key.Day)).ToList();
            foreach (KeyValuePair<DateTime, List<UserBirthday>> userBirthdayKeyValuePair in sortedBirthdays)
            {
                foreach (UserBirthday userBirthday in userBirthdayKeyValuePair.Value)
                {
                    string day = userBirthday.BirthDate.Day > 9
                        ? userBirthday.BirthDate.Day.ToString()
                        : $"0{userBirthday.BirthDate.Day}";
                    string month = userBirthday.BirthDate.Month > 9
                        ? userBirthday.BirthDate.Month.ToString()
                        : $"0{userBirthday.BirthDate.Month}";

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

            UserBirthday userBirthday = new UserBirthday
            {
                GuildID = command.GuildId,
                UserID = birthdayTargetSocketUser.Id,
                UserMention = birthdayTargetSocketUser.Mention,
                Name = birthdayTargetSocketUser.GlobalName ?? birthdayTargetSocketUser.Username,
                BirthDate = new DateTime(parsedDate.Year, parsedDate.Month, parsedDate.Day)
            };

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