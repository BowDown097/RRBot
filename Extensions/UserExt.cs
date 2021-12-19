namespace RRBot.Extensions;
public static class UserExt
{
    public static IGuild GetGuild(this IUser user) => (user as IGuildUser)?.Guild;

    public static IReadOnlyCollection<ulong> GetRoleIds(this IUser user) => (user as IGuildUser)?.RoleIds;

    public static async Task<IUserMessage> NotifyAsync(this IUser user, IMessageChannel channel, string message, bool doDM = false)
    {
        DbUser dbUser = await DbUser.GetById(user.GetGuild().Id, user.Id);

        if (doDM && dbUser.DMNotifs)
            return await user.SendMessageAsync(message);

        if (!dbUser.NoReplyPings)
            message = $"{user.Mention}, {char.ToLower(message[0]) + message[1..]}";
        return await channel.SendMessageAsync(message);
    }

    public static string Sanitize(this IUser user) => RRFormat.Sanitize(user);
}