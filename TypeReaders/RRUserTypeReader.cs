namespace RRBot.TypeReaders;
/// <summary>
///     A UserTypeReader, but StartsWith is used instead of Equals for names, and supports grabbing from REST
/// </summary>
public class RrUserTypeReader : TypeReader
{
    /// <inheritdoc />
    public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
    {
        Dictionary<ulong, TypeReaderValue> results = [];
        IAsyncEnumerable<IUser> channelUsers = context.Channel.GetUsersAsync(CacheMode.CacheOnly).Flatten();
        IReadOnlyCollection<IGuildUser> guildUsers = [];

        if (context.Guild is not null)
            guildUsers = await context.Guild.GetUsersAsync(CacheMode.CacheOnly).ConfigureAwait(false);

        //By Mention (1.0)
        if (MentionUtils.TryParseUser(input, out ulong id))
            await HandleUserId(results, id, context);

        //By Id (0.9)
        if (ulong.TryParse(input, NumberStyles.None, CultureInfo.InvariantCulture, out id))
            await HandleUserId(results, id, context);

        //By Global (Display) Name (0.7-0.8)
        {
            await channelUsers
                .Where(x => x.GlobalName?.StartsWith(input, StringComparison.OrdinalIgnoreCase) == true)
                .ForEachAsync(channelUser => AddResult(results, channelUser as IGuildUser, channelUser.GlobalName == input ? 0.85f : 0.75f))
                .ConfigureAwait(false);

            foreach (IGuildUser guildUser in guildUsers.Where(x => x.GlobalName?.StartsWith(input, StringComparison.OrdinalIgnoreCase) == true))
                AddResult(results, guildUser, guildUser.Username == input ? 0.80f : 0.70f);
        }

        //By Username (0.7-0.8)
        {
            await channelUsers
                .Where(x => x.Username.StartsWith(input, StringComparison.OrdinalIgnoreCase))
                .ForEachAsync(channelUser => AddResult(results, channelUser as IGuildUser, channelUser.Username == input ? 0.85f : 0.75f))
                .ConfigureAwait(false);

            foreach (IGuildUser guildUser in guildUsers.Where(x => x.Username.StartsWith(input, StringComparison.OrdinalIgnoreCase)))
                AddResult(results, guildUser, guildUser.Username == input ? 0.80f : 0.70f);
        }

        //By Nickname (0.7-0.8)
        {
            await channelUsers
                .Where(x => (x as IGuildUser)?.Nickname?.StartsWith(input, StringComparison.OrdinalIgnoreCase) == true)
                .ForEachAsync(channelUser => AddResult(results, channelUser as IGuildUser, (channelUser as IGuildUser)?.Nickname == input ? 0.85f : 0.75f))
                .ConfigureAwait(false);

            foreach (IGuildUser guildUser in guildUsers.Where(x => x.Nickname?.StartsWith(input, StringComparison.OrdinalIgnoreCase) == true))
                AddResult(results, guildUser, guildUser.Nickname == input ? 0.80f : 0.70f);
        }

        return results.Count switch
        {
            1 => TypeReaderResult.FromSuccess([..results.Values]),
            > 1 => TypeReaderResult.FromError((CommandError)9,
                "Your user input is ambiguous. " +
                $"Run the command again, but this time with the user being one of these {results.Values.Count} results:\n" +
                string.Join(", ", results.Values.Select(trv => $"**{trv}**"))),
            _ => TypeReaderResult.FromError(CommandError.ObjectNotFound, "User not found.")
        };
    }

    private static void AddResult(Dictionary<ulong, TypeReaderValue> results, IUser? user, float score)
    {
        if (user is not null && !results.ContainsKey(user.Id))
            results.Add(user.Id, new TypeReaderValue(user, score));
    }

    private static async Task HandleUserId(
        Dictionary<ulong, TypeReaderValue> results, ulong userId, ICommandContext context)
    {
        if (results.ContainsKey(userId))
            return;

        IUser? user;
        if (context.Guild is not null && (user = await context.Guild.GetUserAsync(userId, CacheMode.CacheOnly).ConfigureAwait(false)) is not null)
            results.Add(userId, new TypeReaderValue(user, 1.00f));
        else if ((user = await context.Channel.GetUserAsync(userId, CacheMode.CacheOnly).ConfigureAwait(false)) is not null)
            results.Add(userId, new TypeReaderValue(user, 1.00f));
        else if ((user = await context.Client.GetUserAsync(userId).ConfigureAwait(false)) is not null)
            results.Add(userId, new TypeReaderValue(user, 1.00f));
    }
}