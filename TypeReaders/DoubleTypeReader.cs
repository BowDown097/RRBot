namespace RRBot.TypeReaders;
public class DoubleTypeReader : TypeReader
{
    public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
    {
        if (!input.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return !double.TryParse(input, out double cash)
                ? TypeReaderResult.FromError(CommandError.ParseFailed, $"\"{input}\" is not a double value.")
                : TypeReaderResult.FromSuccess(cash);
        }

        DbUser user = await DbUser.GetById(context.Guild.Id, context.User.Id);
        string[] components = context.Message.Content.Split(' ');
        string command = components[0].Replace(Constants.Prefix, "").ToLower();
        switch (command)
        {
            case "hack":
            case "withdraw":
                string crypto = command == "hack" ? components[2] : components[1];
                string abbreviation = Investments.ResolveAbbreviation(crypto);
                return abbreviation is null
                    ? TypeReaderResult.FromError(CommandError.ParseFailed, $"**{crypto}** is not a currently accepted currency!")
                    : TypeReaderResult.FromSuccess((double)user[abbreviation]);
            case "withdrawvault":
                DbGang gang = await DbGang.GetByName(context.Guild.Id, user.Gang);
                return TypeReaderResult.FromSuccess(gang.VaultBalance);
            default:
                return TypeReaderResult.FromSuccess(user.Cash);
        }
    }
}