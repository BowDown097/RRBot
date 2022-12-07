namespace RRBot.Extensions;
public static class UserExt
{
    public static IGuild GetGuild(this IUser user) => (user as IGuildUser)?.Guild;

    public static IEnumerable<ulong> GetRoleIds(this IUser user) => (user as IGuildUser)?.RoleIds;

    public static async Task NotifyAsync(this IUser user, IMessageChannel channel, string message, bool doDm = false)
    {
        DbUser dbUser = await MongoManager.FetchUserAsync(user.Id, user.GetGuild().Id);
        if (doDm && dbUser.DmNotifs)
        {
            await user.SendMessageAsync(message);
            return;
        }

        if (dbUser.WantsReplyPings)
            message = $"{user.Mention}, {char.ToLower(message[0]) + message[1..]}";

        await channel.SendMessageAsync(message, allowedMentions: Constants.Mentions);
    }

    public static string Sanitize(this IUser user) => StringCleaner.Sanitize(user.ToString());
}