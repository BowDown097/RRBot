namespace RRBot.TypeReaders;
/// <summary>
///     A UserTypeReader, but StartsWith is used instead of Equals for usernames/nicknames
/// </summary>
public class RrGuildUserTypeReader : TypeReader
{
    /// <inheritdoc />
    public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
    {
        var results = new Dictionary<ulong, TypeReaderValue>();
        IUser[] channelUsers = await context.Channel.GetUsersAsync(CacheMode.CacheOnly).Flatten().ToArrayAsync();
        IReadOnlyCollection<IGuildUser> guildUsers = ImmutableArray.Create<IGuildUser>();

        if (context.Guild is not null)
            guildUsers = await context.Guild.GetUsersAsync(CacheMode.CacheOnly).ConfigureAwait(false);

        //By Mention (1.0)
        if (MentionUtils.TryParseUser(input, out var id))
        {
            if (context.Guild is not null)
                AddResult(results, await context.Guild.GetUserAsync(id, CacheMode.CacheOnly).ConfigureAwait(false), 1.00f);
            else
                AddResult(results, await context.Channel.GetUserAsync(id, CacheMode.CacheOnly).ConfigureAwait(false) as IGuildUser, 1.00f);
        }

        //By Id (0.9)
        if (ulong.TryParse(input, NumberStyles.None, CultureInfo.InvariantCulture, out id))
        {
            if (context.Guild is not null)
                AddResult(results, await context.Guild.GetUserAsync(id, CacheMode.CacheOnly).ConfigureAwait(false), 0.90f);
            else
                AddResult(results, await context.Channel.GetUserAsync(id, CacheMode.CacheOnly).ConfigureAwait(false) as IGuildUser, 0.90f);
        }

        //By Username + Discriminator (0.7-0.85)
        int index = input.LastIndexOf('#');
        if (index >= 0)
        {
            string username = input[..index];
            if (ushort.TryParse(input[(index + 1)..], out ushort discriminator))
            {
                var channelUser = Array.Find(channelUsers, x => x.DiscriminatorValue == discriminator &&
                    string.Equals(username, x.Username, StringComparison.OrdinalIgnoreCase)) as IGuildUser;
                AddResult(results, channelUser, channelUser?.Username == username ? 0.85f : 0.75f);
                
                var guildUser = guildUsers.FirstOrDefault(x => x.DiscriminatorValue == discriminator &&
                    string.Equals(username, x.Username, StringComparison.OrdinalIgnoreCase));
                AddResult(results, guildUser, guildUser?.Username == username ? 0.80f : 0.70f);
            }
        }

        //By Username (0.5-0.6)
        {
            foreach (var channelUser in channelUsers.Where(x => x.Username.StartsWith(input, StringComparison.OrdinalIgnoreCase)))
                AddResult(results, channelUser as IGuildUser, channelUser.Username == input ? 0.60f : 0.50f);
            foreach (var guildUser in guildUsers.Where(x => x.Username.StartsWith(input, StringComparison.OrdinalIgnoreCase)))
                AddResult(results, guildUser, guildUser.Username == input ? 0.60f : 0.50f);
        }

        //By Nickname (0.5-0.6)
        {
            foreach (var channelUser in channelUsers.Where(x => (x as IGuildUser)?.Nickname?.StartsWith(input, StringComparison.OrdinalIgnoreCase) == true))
                AddResult(results, channelUser as IGuildUser, (channelUser as IGuildUser)?.Nickname == input ? 0.65f : 0.55f);
            foreach (var guildUser in guildUsers.Where(x => x.Nickname?.StartsWith(input, StringComparison.OrdinalIgnoreCase) == true))
                AddResult(results, guildUser, guildUser.Nickname == input ? 0.60f : 0.50f);
        }

        return results.Count switch
        {
            1 => TypeReaderResult.FromSuccess(results.Values.ToImmutableArray()),
            > 1 => TypeReaderResult.FromError((CommandError)9,
                "Your user input is ambiguous. " +
                $"Run the command again, but this time with the user being one of these {results.Values.Count} results:\n" +
                string.Join(", ", results.Values.Select(trv => $"**{trv}**"))),
            _ => TypeReaderResult.FromError(CommandError.ObjectNotFound, "User not found.")
        };
    }

    private static void AddResult(Dictionary<ulong, TypeReaderValue> results, IGuildUser user, float score)
    {
        if (user is not null && !results.ContainsKey(user.Id))
            results.Add(user.Id, new TypeReaderValue(user, score));
    }
}