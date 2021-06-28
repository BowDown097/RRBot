using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using RRBot.Modules;

namespace RRBot.Extensions
{
    public static class SocketUserExt
    {
        public static async Task<IUserMessage> NotifyAsync(this SocketUser user, ISocketMessageChannel channel, string message, string altMsg = "", bool doDM = false)
        {
            if (doDM)
            {
                bool dmNotify = await UserSettingsGetters.GetDMNotifications(user as IGuildUser);
                string reply = string.IsNullOrEmpty(altMsg) ? message : altMsg;
                if (dmNotify) return await user.SendMessageAsync(reply);
            }

            bool replyPings = await UserSettingsGetters.GetReplyPings(user as IGuildUser);
            if (!string.IsNullOrEmpty(altMsg)) 
                message = replyPings ? altMsg : message;
            else 
                message = replyPings ? $"{user.Mention}, {char.ToLowerInvariant(message[0]) + message.Substring(1)}" : message;

            return await channel.SendMessageAsync(message);
        }
    }
}
