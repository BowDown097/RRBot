namespace RRBot.TypeReaders;
public class DecimalTypeReader : TypeReader
{
    public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
    {
        if (!input.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return !decimal.TryParse(input, out decimal cash)
                ? TypeReaderResult.FromError(CommandError.ParseFailed, $"\"{input}\" is not a decimal value.")
                : TypeReaderResult.FromSuccess(cash);
        }
        
        DbUser user = await MongoManager.FetchUserAsync(context.User.Id, context.Guild.Id);
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
                    : TypeReaderResult.FromSuccess((decimal)user[abbreviation]);
            case "withdrawvault":
                DbGang gang = await MongoManager.FetchGangAsync(user.Gang, context.Guild.Id);
                return TypeReaderResult.FromSuccess(gang.VaultBalance);
            default:
                return TypeReaderResult.FromSuccess(user.Cash);
        }
    }
}