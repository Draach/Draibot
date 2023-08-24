using System.Text;
using Discord;
using Discord.Commands;

namespace Draibot;

[RequireOwner(Group = "Permission")]
public class AdminModule : ModuleBase<SocketCommandContext>
{
    [Command("SetActivityAsync")]
    public Task SetActivityAsync(string type, string name)
    {
        Task resultTask;
        bool isValidEnumValue = Enum.TryParse(type, out ActivityType activityType);


        try
        {
            if (name.Length > 10)
                throw new InvalidActivityNameException();

            if (!isValidEnumValue)
                throw new InvalidActivityTypeException();
            
            resultTask = Context.Client.SetActivityAsync(new CustomActivity()
            {
                Name = name,
                Type = activityType,
            });
        }
        catch (InvalidActivityNameException)
        {
            resultTask = ReplyAsync($"Activity name '{name}' is too long, try using 10 or less characters instead.");
        }
        catch (InvalidActivityTypeException)
        {
            StringBuilder validActivityTypeOptions = new StringBuilder();
            ActivityType[] allEnumValues = (ActivityType[])Enum.GetValues(typeof(ActivityType));

            // Iterate through and print the enum values
            foreach (ActivityType enumValue in allEnumValues)
            {
                validActivityTypeOptions.Append($"[{enumValue}] ");
            }

            validActivityTypeOptions.Remove(validActivityTypeOptions.Length - 1, 1);

            resultTask = ReplyAsync($"{activityType} is not a valid Activity Type. Try one of these: {validActivityTypeOptions}.");
        }

        return resultTask;
    }
}