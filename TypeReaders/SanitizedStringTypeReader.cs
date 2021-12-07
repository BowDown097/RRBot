namespace RRBot.TypeReaders
{
    public class SanitizedStringTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
            => Task.FromResult(TypeReaderResult.FromSuccess(RRFormat.Sanitize(input)));
    }
}
