namespace RRBot.Extensions;
public static class UserExt
{
    public static async Task NotifyAsync(this IUser user, IMessageChannel channel, string message, bool doDm = false)
    {
        if (user is not IGuildUser guildUser)
            return;

        DbUser dbUser = await MongoManager.FetchUserAsync(user.Id, guildUser.GuildId);
        if (doDm && dbUser.DmNotifs)
        {
            await user.SendMessageAsync(message);
            return;
        }

        if (dbUser.WantsReplyPings)
            message = $"{user.Mention}, {char.ToLower(message[0]) + message[1..]}";

        await channel.SendMessageAsync(message, allowedMentions: Constants.Mentions);
    }

    public static string Sanitize(this IUser user)
    {
        return StringCleaner.Sanitize(user?.ToString() ?? "");
    }

    public static async Task<string> SanitizeById(ulong userId, SocketCommandContext context)
    {
        IUser? user;
        if ((user = await context.Channel.GetUserAsync(userId).ConfigureAwait(false)) is not null)
            return user.Sanitize();
        else if ((user = await context.Client.GetUserAsync(userId).ConfigureAwait(false)) is not null)
            return $"{user.Sanitize()} *(left server - ID {userId})*";
        else
            return $"user not found: ID {userId}";
    }
}