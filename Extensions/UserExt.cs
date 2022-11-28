namespace RRBot.Extensions;
public static class UserExt
{
    public static IGuild GetGuild(this IUser user) => (user as IGuildUser)?.Guild;

    public static IReadOnlyCollection<ulong> GetRoleIds(this IUser user) => (user as IGuildUser)?.RoleIds;

    public static async Task<IUserMessage> NotifyAsync(this IUser user, IMessageChannel channel, string message, bool doDm = false)
    {
        DbUser dbUser = await MongoManager.FetchUserAsync(user.Id, user.GetGuild().Id);
        if (doDm && dbUser.DmNotifs)
            return await user.SendMessageAsync(message);
        if (dbUser.WantsReplyPings)
            message = $"{user.Mention}, {char.ToLower(message[0]) + message[1..]}";

        return await channel.SendMessageAsync(message, allowedMentions: Constants.Mentions);
    }

    public static string Sanitize(this IUser user) => Format.Sanitize(user.ToString()).Replace("\\:", ":").Replace("\\/", "/").Replace("\\.", ".");
}