namespace RRBot.Extensions
{
    public static class SocketUserExt
    {
        public static async Task<IUserMessage> NotifyAsync(this SocketUser user, ISocketMessageChannel channel, string message, string altMsg = "", bool doDM = false)
        {
            DbUser dbUser = await DbUser.GetById((ulong)(user as IGuildUser)?.GuildId, user.Id);
            if (doDM)
            {
                string reply = string.IsNullOrEmpty(altMsg) ? message : altMsg;
                if (dbUser.DMNotifs)
                    return await user.SendMessageAsync(reply);
            }

            if (channel == null)
                return null;

            if (!string.IsNullOrEmpty(altMsg))
                message = dbUser.NoReplyPings ? message : altMsg;
            else
                message = dbUser.NoReplyPings ? message : $"{user.Mention}, {char.ToLowerInvariant(message[0]) + message[1..]}";

            return await channel.SendMessageAsync(message);
        }
    }
}
