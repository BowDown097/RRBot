namespace RRBot.Extensions;
public static class CommandInfoExt
{
    public static string GetUsage(this CommandInfo command)
    {
        StringBuilder usageText = new($"${command.Name.ToLower()}");
        foreach (Discord.Commands.ParameterInfo parameter in command.Parameters)
            usageText.Append(parameter.IsOptional ? $" <{parameter}>" : $" [{parameter}]");
        return usageText.ToString();
    }
}