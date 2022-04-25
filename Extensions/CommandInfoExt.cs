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

    public static bool TryGetPrecondition<T>(this CommandInfo command) where T : PreconditionAttribute => command.TryGetPrecondition<T>(out _);
    public static bool TryGetPrecondition<T>(this CommandInfo command, out T precondition) where T : PreconditionAttribute
    {
        T possPrecond = (T)command.Preconditions.FirstOrDefault(cond => cond is T) ?? (T)command.Module.Preconditions.FirstOrDefault(cond => cond is T);
        precondition = possPrecond;
        return possPrecond != null;
    }
}