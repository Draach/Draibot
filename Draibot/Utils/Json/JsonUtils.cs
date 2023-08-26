using Newtonsoft.Json;

namespace Draibot.Utils;

public static class JsonUtils
{
    private static string filePath = "birthdaysData.json";

    private static string ConverToJsonString(object value)
    {
        return JsonConvert.SerializeObject(value, Formatting.Indented);
    }

    public static List<UserBirthday> ReadFromJson()
    {
        string jsonContent = File.ReadAllText(filePath);

        List<UserBirthday> birthdays = new();
        List<UserBirthday>? jsonBirthdays = JsonConvert.DeserializeObject<List<UserBirthday>>(jsonContent);
        if (jsonBirthdays != null)
        {
            birthdays = jsonBirthdays;
        }

        return birthdays;
    }

    public static void AddBirthdayToBirthdaysJson(UserBirthday userBirthday)
    {
        if (!File.Exists(filePath))
        {
            FileStream fileStream = File.Create(filePath);
            fileStream.Close();
        }

        List<UserBirthday> birthdays = ReadFromJson();
        birthdays.Add(userBirthday);
        string birthdaysJsonString = ConverToJsonString(birthdays);

        File.WriteAllText(filePath, birthdaysJsonString);
        Console.WriteLine($"Birthday Saved to Json successfully.");
    }
}