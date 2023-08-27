using Newtonsoft.Json;

namespace Draibot.Utils;

public static class JsonUtils
{
    private static string filePath = "birthdaysData.json";

    private static string ConverToJsonString(object value)
    {
        return JsonConvert.SerializeObject(value, Formatting.Indented);
    }

    public static SortedDictionary<DateTime, List<UserBirthday>> ReadFromJson()
    {
        CreateFileIfNotExists();

        string jsonContent = File.ReadAllText(filePath);

        SortedDictionary<DateTime, List<UserBirthday>> sortedBirthdays =
            new SortedDictionary<DateTime, List<UserBirthday>>();

        SortedDictionary<DateTime, List<UserBirthday>>? jsonBirthdays =
            JsonConvert.DeserializeObject<SortedDictionary<DateTime, List<UserBirthday>>>(jsonContent);
        if (jsonBirthdays != null)
        {
            sortedBirthdays = jsonBirthdays;
        }

        return sortedBirthdays;
    }

    private static void CreateFileIfNotExists()
    {
        if (!File.Exists(filePath))
        {
            FileStream fileStream = File.Create(filePath);
            fileStream.Close();
        }
    }

    public static void AddBirthdayToBirthdaysJson(UserBirthday newUserBirthday)
    {
        SortedDictionary<DateTime, List<UserBirthday>> sortedBirthdays = ReadFromJson();

        if (!sortedBirthdays.ContainsKey(newUserBirthday.BirthDate))
        {
            CreateNewBirthdayEntry(newUserBirthday, sortedBirthdays);
        }
        else
        {
            AddBirthdayToExistingKeyEntry(newUserBirthday, sortedBirthdays);
        }


        string birthdaysJsonString = ConverToJsonString(sortedBirthdays);

        File.WriteAllText(filePath, birthdaysJsonString);
    }

    private static void AddBirthdayToExistingKeyEntry(UserBirthday newUserBirthday, SortedDictionary<DateTime, List<UserBirthday>> sortedBirthdays)
    {
        List<UserBirthday> usersBirthdays = sortedBirthdays[newUserBirthday.BirthDate];
        // Find the index where the new user should be inserted
        int index = usersBirthdays.FindIndex(userBirthday =>
            new DateTime(1, userBirthday.BirthDate.Month, userBirthday.BirthDate.Day) >
            new DateTime(1, newUserBirthday.BirthDate.Month, newUserBirthday.BirthDate.Day));

        if (index == -1)
        {
            usersBirthdays.Add(
                newUserBirthday); // Insert at the end if new user's birthday is later than all existing users
        }
        else
        {
            usersBirthdays.Insert(index, newUserBirthday); // Insert at the appropriate position
        }

        sortedBirthdays[newUserBirthday.BirthDate] = usersBirthdays;
    }

    private static void CreateNewBirthdayEntry(UserBirthday newUserBirthday, SortedDictionary<DateTime, List<UserBirthday>> sortedBirthdays)
    {
        List<UserBirthday> usersBirthdays = new() { newUserBirthday };
        sortedBirthdays.Add(newUserBirthday.BirthDate, usersBirthdays);
    }
}