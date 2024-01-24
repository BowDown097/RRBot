namespace RRBot.TypeReaders;
public class ListTypeReader<T> : TypeReader
{
    public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
    {
        try
        {
            string[] split = input.Contains(',') ? input.Split(',', StringSplitOptions.TrimEntries) : [input];
            List<T> result = split.Select(v => (T)Convert.ChangeType(v, typeof(T))).ToList();
            return Task.FromResult(TypeReaderResult.FromSuccess(result));
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException)
        {
            return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed,
                $"Could not convert input to a {typeof(T).Name} or a list of the type if appropriate. " +
                "If your input is a list, make sure all values are of the same type and are separated by a comma."));
        }
    }
}