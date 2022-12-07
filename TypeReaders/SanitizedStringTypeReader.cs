namespace RRBot.TypeReaders;
public class SanitizedStringTypeReader : TypeReader
{
    public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
    {
        CommandService commands = services.GetRequiredService<CommandService>();
        string[] components = context.Message.Content.Split(' ');
        string commandName = components[0].Replace(Constants.Prefix, "").ToLower();
        CommandInfo command = commands.Search(commandName).Commands[0].Command;

        return command.Attributes.Any(a => a is DoNotSanitizeAttribute)
            ? Task.FromResult(TypeReaderResult.FromSuccess(input))
            : Task.FromResult(TypeReaderResult.FromSuccess(StringCleaner.Sanitize(input)));
    }
}