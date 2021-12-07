namespace RRBot.Extensions;
public static class SocketUserExt
{
    public static async Task<IUserMessage> NotifyAsync(this SocketUser user, ISocketMessageChannel channel, string message, bool doDM = false)
    {
        IGuildUser guildUser = user as IGuildUser;
        DbUser dbUser = await DbUser.GetById(guildUser.GuildId, user.Id);

        if (doDM && dbUser.DMNotifs)
            return await user.SendMessageAsync(message);

        if (!dbUser.NoReplyPings)
            message = $"{user.Mention}, {char.ToLower(message[0]) + message[1..]}";
        return await channel.SendMessageAsync(message);
    }
}